// ============= Load sản phẩm từ API + Popup mô tả =============
async function loadProducts() {
  const list = document.getElementById("product-list");
  list.innerHTML = "<h3>Đang tải sản phẩm...</h3>";

  try {
    const res = await fetch("http://localhost:5114/api/product");
    if (!res.ok) {
      list.innerHTML = "<h3>Không thể tải danh sách sản phẩm.</h3>";
      return;
    }

    const data = await res.json();
    list.innerHTML = "";

    data.forEach(p => {
      const imageUrl = `http://localhost:5114/Images/${p.id}.jpg`;

      const card = document.createElement("div");
      card.className = "card";
      card.innerHTML = `
        <img src="${imageUrl}" alt="${p.name}" onerror="this.src='https://via.placeholder.com/300x200';" />
        <h3>${p.name}</h3>
        <div class="price">${p.price.toLocaleString()} đ</div>
        <div class="desc">
          ${p.description && p.description.length > 100 
            ? p.description.slice(0, 100) + "..." 
            : p.description || "Không có mô tả"}
          ${p.description && p.description.length > 100 
            ? `<span class="detail-btn" onclick="showDescription(${p.id}, '${p.name.replace(/'/g, "\\'")}', \`${p.description.replace(/`/g, "\\`")}\`)">Xem chi tiết</span>`
            : ""}
        </div>
        <div class="stock">
          Tồn kho: ${p.stock > 0 ? p.stock : "<span style='color:red'>Hết hàng</span>"}
        </div>
        <button onclick="addToCart(${p.id}, '${p.name.replace(/'/g, "\\'")}', ${p.price}, '${imageUrl}')" 
          ${p.stock === 0 ? "disabled" : ""}>
          Thêm vào giỏ
        </button>
      `;
      list.appendChild(card);
    });

  } catch (err) {
    console.error("Lỗi tải sản phẩm:", err);
    list.innerHTML = "<h3>Có lỗi xảy ra khi tải sản phẩm.</h3>";
  }
}

// Hàm hiển thị popup mô tả chi tiết
function showDescription(productId, productName, fullDesc) {
  const modal = document.getElementById("descModal");
  const title = document.getElementById("descModalTitle");
  const content = document.getElementById("descModalContent");

  title.textContent = productName;
  content.innerHTML = fullDesc.replace(/\n/g, '<br>'); // hỗ trợ xuống dòng

  modal.classList.add("active");
}

// Đóng popup
document.addEventListener("click", (e) => {
  const modal = document.getElementById("descModal");
  if (e.target === modal || e.target.classList.contains("close-desc")) {
    modal.classList.remove("active");
  }
});

loadProducts();
updateCartBadge(); // giữ nguyên

// ================== GIỎ HÀNG RIÊNG CHO TỪNG USER + CẬP NHẬT BADGE ==================

// Lấy giỏ hàng theo userID
function getUserCart() {
  const user = JSON.parse(localStorage.getItem('currentUser') || '{}');
  if (user && user.userID) {
    const saved = localStorage.getItem(`cart_${user.userID}`);
    return saved ? JSON.parse(saved) : [];
  }
  return [];
}

// Lưu giỏ hàng theo userID
function saveUserCart(cartArray) {
  const user = JSON.parse(localStorage.getItem('currentUser') || '{}');
  if (user && user.userID) {
    localStorage.setItem(`cart_${user.userID}`, JSON.stringify(cartArray));
  }
}

// Cập nhật badge số lượng trên menu (các trang có .cart-badge)
function updateCartBadge() {
  const badge = document.querySelector('.cart-badge');
  if (badge) {
    const cart = getUserCart();
    const total = cart.reduce((sum, item) => sum + item.quantity, 0);
    badge.textContent = total;
    badge.style.display = total > 0 ? 'flex' : 'none';
  }
}

// Hàm thêm vào giỏ hàng (gọi từ nút)
function addToCart(id, name, price, imageUrl) {
  // Kiểm tra đăng nhập
  const user = JSON.parse(localStorage.getItem('currentUser') || '{}');
  if (!user.userID) {
    if (confirm("Bạn cần đăng nhập để thêm vào giỏ hàng!\nChuyển đến trang đăng nhập?")) {
      window.location.href = "LogReg.html";
    }
    return;
  }

  let cart = getUserCart();

  const exist = cart.find(x => x.id === id);
  if (exist) {
    exist.quantity += 1;
  } else {
    cart.push({
      id: id,
      name: name,
      price: price,
      imageUrl: imageUrl,
      quantity: 1,
      checked: true  // mặc định chọn để thanh toán
    });
  }

  saveUserCart(cart);
  alert(`Đã thêm "${name}" vào giỏ hàng!`);
  updateCartBadge(); // cập nhật badge ngay lập tức
}

// Cập nhật badge khi trang load (nếu đã đăng nhập)
document.addEventListener("DOMContentLoaded", function () {
  updateCartBadge();
});