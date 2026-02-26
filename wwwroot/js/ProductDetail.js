const urlParams = new URLSearchParams(window.location.search);
const productId = urlParams.get('id');

if (!productId) {
  document.body.innerHTML = "<h1 style='text-align:center;padding:100px;'>Không tìm thấy sản phẩm</h1>";
}

let selectedColorId = null;
let selectedSizeName = null;
let quantity = 1;
let productData = null;

async function loadProductDetail() {
  try {
    const res = await fetch(`http://localhost:5000/api/admin/products/${productId}`);
    const result = await res.json();

    if (!result.success || !result.data) {
      document.body.innerHTML = "<h1>Lỗi tải chi tiết sản phẩm</h1>";
      return;
    }

    productData = result.data;

    // Tên & Giá
    document.getElementById("productName").textContent = productData.name;
    document.getElementById("productPrice").textContent = productData.price.toLocaleString('vi-VN') + " đ";

    // Mô tả
    document.getElementById("productDescription").textContent = productData.description || "Không có mô tả";

    // Ảnh gallery
    const gallery = document.getElementById("imageGallery");
    gallery.innerHTML = "";
    if (productData.images && productData.images.length > 0) {
      productData.images.forEach(img => {
        const imgEl = document.createElement("img");
        imgEl.src = `http://localhost:5000${img.imageUrl}`;
        imgEl.alt = "Ảnh sản phẩm";
        imgEl.onerror = () => imgEl.src = "https://via.placeholder.com/400x400?text=Error";
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
      let cls = 'filled';
      if (i >= full) cls = hasHalf && i === full ? 'half' : '';
      stars += `<svg class="${cls}" viewBox="0 0 24 24"><path d="M12 17.27L18.18 21l-1.64-7.03L22 9.24l-7.19-.61L12 2 9.19 8.63 2 9.24l5.46 4.73L5.82 21z"/></svg>`;
    }
    document.getElementById("starRating").innerHTML = stars;
    document.getElementById("ratingText").textContent = `(${rating.toFixed(1)} / 5)`;

    // Màu sắc (dùng Colors từ response)
    const colorDots = document.getElementById("colorDots");
    colorDots.innerHTML = "";
    if (productData.colors && productData.colors.length > 0) {
      productData.colors.forEach(c => {
        const dot = document.createElement("div");
        dot.className = "color-dot";
        dot.style.backgroundColor = c.hexCode;
        dot.title = c.name;
        dot.onclick = () => {
          selectedColorId = c.hexCode; // dùng hexCode làm key tạm
          document.querySelectorAll('.color-dot').forEach(d => d.classList.remove('selected'));
          dot.classList.add('selected');
          document.getElementById("selectedColorName").textContent = `Màu đã chọn: ${c.name}`;
        };
        colorDots.appendChild(dot);
      });
    } else {
      colorDots.innerHTML = "<p>Chưa có màu</p>";
    }

    // Kích thước (dùng Sizes từ response)
    const sizeOptions = document.getElementById("sizeOptions");
    sizeOptions.innerHTML = "";
    if (productData.sizes && productData.sizes.length > 0) {
      productData.sizes.forEach(sizeName => {
        const btn = document.createElement("button");
        btn.className = "size-btn";
        btn.textContent = sizeName;
        btn.onclick = () => {
          selectedSizeName = sizeName;
          document.querySelectorAll('.size-btn').forEach(b => b.classList.remove('selected'));
          btn.classList.add('selected');
        };
        sizeOptions.appendChild(btn);
      });
    } else {
      sizeOptions.innerHTML = "<p>Chưa có kích thước</p>";
    }

    // Tồn kho (tạm tổng Quantity từ Variants)
    const totalStock = productData.variants?.reduce((sum, v) => sum + (v.quantity || 0), 0) || 0;
    const stockEl = document.getElementById("stockStatus");
    stockEl.textContent = totalStock > 0 ? "Còn hàng" : "Hết hàng";
    stockEl.className = totalStock > 0 ? "stock-status in-stock" : "stock-status out-of-stock";

  } catch (err) {
    console.error(err);
    document.body.innerHTML = "<h1>Lỗi kết nối server</h1>";
  }
}

// Số lượng
function changeQuantity(delta) {
  quantity = Math.max(1, quantity + delta);
  document.getElementById("quantity").value = quantity;
}

// Thêm vào giỏ
function addToCartDetail() {
  if (!selectedColorId) {
    alert("Vui lòng chọn màu!");
    return;
  }
  if (!selectedSizeName) {
    alert("Vui lòng chọn kích thước!");
    return;
  }

  const user = JSON.parse(localStorage.getItem('currentUser') || '{}');
  if (!user.userId) {
    if (confirm("Bạn cần đăng nhập để thêm vào giỏ!")) {
      window.location.href = "LogReg.html";
    }
    return;
  }

  let cart = JSON.parse(localStorage.getItem(`cart_${user.userId}`) || '[]');

  const item = {
    id: productId,
    name: productData.name,
    price: productData.price,
    imageUrl: productData.thumbnail ? `http://localhost:5000/Images/${productData.thumbnail}` : "",
    quantity: quantity,
    colorHex: selectedColorId,
    sizeName: selectedSizeName,
    checked: true
  };

  const exist = cart.find(x => x.id === productId && x.colorHex === selectedColorId && x.sizeName === selectedSizeName);
  if (exist) exist.quantity += quantity;
  else cart.push(item);

  localStorage.setItem(`cart_${user.userId}`, JSON.stringify(cart));
  alert("Đã thêm vào giỏ hàng!");
}

// Load
document.addEventListener("DOMContentLoaded", loadProductDetail);