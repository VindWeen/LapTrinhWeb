const urlParams = new URLSearchParams(window.location.search);
const productId = urlParams.get('id');

if (!productId) {
  document.body.innerHTML = "<h1 style='text-align:center;padding:100px;'>Không tìm thấy sản phẩm</h1>";
}

let selectedColorId = null;
let selectedSizeId = null;
let quantity = 1;
let productData = null;
let cartItems = []; // Lưu tạm giỏ hàng từ server để tính tồn kho realtime

// Hàm lấy giỏ hàng từ server
async function fetchCartItems() {
  const token = localStorage.getItem('token');
  if (!token) return [];

  try {
    const res = await fetch('http://localhost:5000/api/cart', {
      headers: { 'Authorization': `Bearer ${token}` }
    });
    const result = await res.json();
    if (result.success) {
      cartItems = result.data;
    } else {
      cartItems = [];
    }
  } catch (err) {
    console.error('Lỗi lấy giỏ hàng:', err);
    cartItems = [];
  }
}

// Tính tồn kho thực tế của variant (trừ giỏ hàng)
function getRealStock(colorId, sizeId) {
  if (!productData.variants) return 0;

  const variant = productData.variants.find(v => 
    v.colorId === colorId && v.sizeId === sizeId
  );
  if (!variant) return 0;

  // Tồn kho gốc trong kho
  let stock = variant.quantity || 0;

  // Trừ số lượng đang có trong giỏ hàng của user
  const cartItem = cartItems.find(item => 
    item.variantId === variant.id
  );
  if (cartItem) {
    stock -= cartItem.quantity;
  }

  return Math.max(0, stock); // Không âm
}

// Cập nhật UI tồn kho khi chọn variant
function updateStockStatus() {
  const stockEl = document.getElementById("stockStatus");
  if (!stockEl) return;

  if (!selectedColorId || !selectedSizeId) {
    stockEl.textContent = "Vui lòng chọn màu và kích thước";
    stockEl.className = "stock-status";
    return;
  }

  const realStock = getRealStock(selectedColorId, selectedSizeId);
  if (realStock > 0) {
    stockEl.textContent = `Còn hàng: ${realStock}`;
    stockEl.className = "stock-status in-stock";
  } else {
    stockEl.textContent = "Hết hàng";
    stockEl.className = "stock-status out-of-stock";
  }

  updateAddToCartButton();
  updateQuantityLimit(realStock);
}

// Giới hạn số lượng input và +/- theo tồn kho
function updateQuantityLimit(maxStock) {
  const qtyInput = document.getElementById("quantity");
  if (!qtyInput) return;

  qtyInput.max = maxStock;
  if (quantity > maxStock) {
    quantity = maxStock;
    qtyInput.value = quantity;
    if (maxStock > 0) {
      alert(`Chỉ còn ${maxStock} sản phẩm cho biến thể này!`);
    }
  }
}

function changeQuantity(delta) {
  const qtyInput = document.getElementById("quantity");
  const maxStock = selectedColorId && selectedSizeId 
    ? getRealStock(selectedColorId, selectedSizeId) 
    : 0;

  let newQty = quantity + delta;
  newQty = Math.max(1, newQty);
  newQty = Math.min(newQty, maxStock || 999); // Giới hạn max tồn kho

  quantity = newQty;
  qtyInput.value = quantity;

  if (delta > 0 && maxStock > 0 && newQty === maxStock) {
    alert(`Đã đạt số lượng tối đa còn lại: ${maxStock}`);
  }
}

// Cập nhật nút thêm giỏ
function updateAddToCartButton() {
  const addBtn = document.getElementById("addToCartBtn");
  if (!addBtn) return;

  const realStock = selectedColorId && selectedSizeId 
    ? getRealStock(selectedColorId, selectedSizeId) 
    : 0;

  addBtn.disabled = !(selectedColorId && selectedSizeId && realStock > 0);
  addBtn.classList.toggle('disabled', addBtn.disabled);
}

// Các hàm updateSizes, updateColors giữ nguyên (chỉ thêm gọi updateStockStatus)
function updateSizes() {
  const sizeOptions = document.getElementById("sizeOptions");
  if (!sizeOptions || !productData.sizes) return;

  sizeOptions.querySelectorAll('.size-btn').forEach(btn => {
    const sizeId = parseInt(btn.dataset.sizeId);
    const stock = selectedColorId 
      ? getRealStock(selectedColorId, sizeId) 
      : productData.variants?.some(v => v.sizeId === sizeId && v.quantity > 0) ? 999 : 0;

    btn.disabled = stock <= 0;
    btn.classList.toggle('disabled', stock <= 0);

    if (btn.dataset.sizeId == selectedSizeId && stock <= 0) {
      btn.classList.remove('selected');
      selectedSizeId = null;
    }
  });

  updateAddToCartButton();
  updateStockStatus(); // Cập nhật tồn kho
}

function updateColors() {
  const colorDots = document.getElementById("colorDots");
  if (!colorDots || !productData.colors) return;

  colorDots.querySelectorAll('.color-dot').forEach(dot => {
    const colorId = parseInt(dot.dataset.colorId);
    const stock = selectedSizeId 
      ? getRealStock(colorId, selectedSizeId) 
      : productData.variants?.some(v => v.colorId === colorId && v.quantity > 0) ? 999 : 0;

    dot.style.opacity = stock > 0 ? '1' : '0.4';
    dot.style.pointerEvents = stock > 0 ? 'auto' : 'none';

    if (dot.dataset.colorId == selectedColorId && stock <= 0) {
      dot.classList.remove('selected');
      selectedColorId = null;
    }
  });

  updateAddToCartButton();
  updateStockStatus(); // Cập nhật tồn kho
}

async function loadProductDetail() {
  try {
    const res = await fetch(`http://localhost:5000/api/admin/products/${productId}`);
    const result = await res.json();

    if (!result.success || !result.data) {
      document.body.innerHTML = "<h1>Lỗi tải chi tiết sản phẩm</h1>";
      return;
    }

    productData = result.data;

    document.getElementById("productName").textContent = productData.name;
    document.getElementById("productPrice").textContent = productData.price.toLocaleString('vi-VN') + " đ";
    document.getElementById("productDescription").textContent = productData.description || "Không có mô tả";

    // Ảnh gallery
    const gallery = document.getElementById("imageGallery");
    gallery.innerHTML = "";
    if (productData.images && productData.images.length > 0) {
      productData.images.forEach(img => {
        const imgEl = document.createElement("img");
        imgEl.src = `http://localhost:5000${img.imageUrl}`;
        imgEl.alt = "Ảnh sản phẩm";
        imgEl.onerror = () => imgEl.src = 'https://placehold.co/400x400?text=Error+Image';
        gallery.appendChild(imgEl);
      });
    } else {
      gallery.innerHTML = "<p>Chưa có ảnh</p>";
    }

    // Rating sao
    const rating = productData.averageRating || 0;
    let stars = '';
    const full = Math.floor(rating);
    const hasHalf = rating % 1 >= 0.5;
    for (let i = 0; i < 5; i++) {
      let cls = i < full ? 'filled' : (i === full && hasHalf ? 'half' : '');
      stars += `<svg class="${cls}" viewBox="0 0 24 24"><path d="M12 17.27L18.18 21l-1.64-7.03L22 9.24l-7.19-.61L12 2 9.19 8.63 2 9.24l5.46 4.73L5.82 21z"/></svg>`;
    }
    document.getElementById("starRating").innerHTML = stars;
    document.getElementById("ratingText").textContent = `(${rating.toFixed(1)} / 5)`;

    // Màu sắc
    const colorDots = document.getElementById("colorDots");
    colorDots.innerHTML = "";
    if (productData.colors && productData.colors.length > 0) {
      productData.colors.forEach(c => {
        const dot = document.createElement("div");
        dot.className = "color-dot";
        dot.style.backgroundColor = c.hexCode;
        dot.title = c.name;
        dot.dataset.colorId = c.id;
        dot.onclick = () => {
          const clickedId = parseInt(c.id);
          if (selectedColorId === clickedId) {
            selectedColorId = null;
            dot.classList.remove('selected');
          } else {
            selectedColorId = clickedId;
            document.querySelectorAll('.color-dot').forEach(d => d.classList.remove('selected'));
            dot.classList.add('selected');
            document.getElementById("selectedColorName").textContent = `Màu đã chọn: ${c.name}`;
          }
          updateSizes();
        };
        colorDots.appendChild(dot);
      });
    } else {
      colorDots.innerHTML = "<p>Chưa có màu</p>";
    }

    // Kích thước
    const sizeOptions = document.getElementById("sizeOptions");
    sizeOptions.innerHTML = "";
    if (productData.sizes && productData.sizes.length > 0) {
      productData.sizes.forEach(s => {
        const btn = document.createElement("button");
        btn.className = "size-btn";
        btn.textContent = s.name;
        btn.dataset.sizeId = s.id;
        btn.onclick = () => {
          const clickedId = parseInt(s.id);
          if (selectedSizeId === clickedId) {
            selectedSizeId = null;
            btn.classList.remove('selected');
          } else {
            selectedSizeId = clickedId;
            document.querySelectorAll('.size-btn').forEach(b => b.classList.remove('selected'));
            btn.classList.add('selected');
          }
          updateColors();
        };
        sizeOptions.appendChild(btn);
      });
    } else {
      sizeOptions.innerHTML = "<p>Chưa có kích thước</p>";
    }

    // Load giỏ hàng lần đầu để tính tồn kho
    await fetchCartItems();

    // Khởi tạo UI
    updateSizes();
    updateColors();
    updateStockStatus();
  } catch (err) {
    console.error(err);
    document.body.innerHTML = "<h1>Lỗi kết nối server</h1>";
  }
}

async function addToCartDetail() {
  if (!selectedColorId || !selectedSizeId) {
    alert("Vui lòng chọn màu và kích thước!");
    return;
  }

  let variantId = null;
  if (productData.variants && productData.variants.length > 0) {
    const selectedVariant = productData.variants.find(v => 
      v.colorId === selectedColorId && v.sizeId === selectedSizeId
    );
    if (selectedVariant) variantId = selectedVariant.id;
  }

  if (!variantId) {
    alert("Không tìm thấy biến thể phù hợp!");
    return;
  }

  const realStock = getRealStock(selectedColorId, selectedSizeId);
  if (realStock <= 0) {
    alert("Biến thể này đã hết hàng!");
    return;
  }
  if (quantity > realStock) {
    alert(`Chỉ còn ${realStock} sản phẩm!`);
    quantity = realStock;
    document.getElementById("quantity").value = quantity;
    return;
  }

  const token = localStorage.getItem('token');
  if (!token) {
    if (confirm("Bạn cần đăng nhập để thêm vào giỏ!")) {
      window.location.href = "LogReg.html";
    }
    return;
  }

  try {
    const res = await fetch('http://localhost:5000/api/cart', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
      },
      body: JSON.stringify({
        ProductId: parseInt(productId),
        ProductVariantId: variantId,
        Quantity: quantity
      })
    });

    const result = await res.json();

    if (res.ok && result.success) {
      alert("Đã thêm vào giỏ hàng!");
      // Reload giỏ hàng để cập nhật tồn kho realtime
      await fetchCartItems();
      updateStockStatus();
      // Optional: Cập nhật badge giỏ hàng toàn site
      // await updateCartBadge(); (nếu bạn có hàm này ở file khác)
    } else {
      alert(result.message || 'Lỗi khi thêm vào giỏ hàng');
    }
  } catch (err) {
    console.error('Lỗi gọi API thêm vào giỏ:', err);
    alert('Lỗi kết nối server. Vui lòng thử lại!');
  }
}

document.addEventListener("DOMContentLoaded", loadProductDetail);