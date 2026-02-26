// ============= Load sản phẩm từ API mới + Popup mô tả =============
let currentPage = 1;
const pageSize = 12; // Có thể đổi tùy ý

async function loadProducts(page = 1) {
  const list = document.getElementById("product-list");
  list.innerHTML = "<h3>Đang tải sản phẩm...</h3>";

  try {
    const url = `http://localhost:5000/api/admin/products?page=${page}&pageSize=${pageSize}&search=`;
    
    const res = await fetch(url);
    const result = await res.json();

    if (!result.success || !result.data) {
      list.innerHTML = "<h3>Không tải được sản phẩm.</h3>";
      return;
    }

    const products = result.data;
    list.innerHTML = "";

    products.forEach(p => {
      const imageUrl = p.thumbnail 
        ? `http://localhost:5000/Images/${p.thumbnail}` 
        : "https://via.placeholder.com/300x240?text=No+Image";

      const formattedPrice = (p.price || 0).toLocaleString('vi-VN');

      // Rating sao
      const rating = p.averageRating || 0;
      const fullStars = Math.floor(rating);
      const hasHalf = rating % 1 >= 0.5;
      let starsHTML = '';
      for (let i = 0; i < 5; i++) {
        let cls = 'star';
        if (i < fullStars) cls += ' filled';
        else if (i === fullStars && hasHalf) cls += ' half';
        starsHTML += `<svg class="${cls}" viewBox="0 0 24 24"><path d="M12 17.27L18.18 21l-1.64-7.03L22 9.24l-7.19-.61L12 2 9.19 8.63 2 9.24l5.46 4.73L5.82 21z"/></svg>`;
      }

      // Color dots
      let colorsHTML = '';
      if (p.colors && p.colors.length > 0) {
        colorsHTML = p.colors.map(c => `
          <div class="color-dot" style="background-color: ${c.hexCode}" 
               title="${c.name}"></div>
        `).join('');
      }

      const card = document.createElement("div");
      card.className = "card";
      // Thêm cursor pointer và onclick chuyển trang chi tiết
      card.style.cursor = "pointer";

      // Click toàn card để đi chi tiết (trừ nút "Thêm vào giỏ")
      card.onclick = (e) => {
        // Ngăn click nút thêm giỏ hoặc các phần con khác
        if (!e.target.closest('button') && !e.target.closest('.detail-btn')) {
          window.location.href = `ProductDetail.html?id=${p.id}`;
        }
      };
      card.innerHTML = `
        <img src="${imageUrl}" alt="${p.name}" onerror="this.src='https://via.placeholder.com/300x240?text=Error';" />
        <h3>${p.name}</h3>
        
        <div class="rating">
          ${starsHTML}
          <span class="size-range">${p.sizeRange || ''}</span>
        </div>

        <div class="color-dots">${colorsHTML}</div>

        <div class="price">${formattedPrice} đ</div>
        <div class="desc">
          ${p.description && p.description.length > 100 
            ? p.description.slice(0, 100) + "..." 
            : p.description || "Không có mô tả"}
          ${p.description && p.description.length > 100 
            ? `<span class="detail-btn" onclick="showDescription(${p.id}, '${p.name.replace(/'/g, "\\'")}', \`${(p.description || '').replace(/`/g, "\\`")}\`)">Xem chi tiết</span>`
            : ""}
        </div>
        <div class="stock">Tồn kho: <span style="color:#10b981">Có sẵn</span></div>
        <button onclick="addToCart(${p.id}, '${p.name.replace(/'/g, "\\'")}', ${p.price}, '${imageUrl}')">
          Thêm vào giỏ
        </button>
      `;
      list.appendChild(card);
    });
  } catch (err) {
    console.error(err);
    list.innerHTML = "<h3>Lỗi kết nối server.</h3>";
  }
}

// Hàm hiển thị popup mô tả chi tiết
function showDescription(productId, productName, fullDesc) {
  const modal = document.getElementById("descModal");
  const title = document.getElementById("descModalTitle");
  const content = document.getElementById("descModalContent");

  title.textContent = productName;
  content.innerHTML = fullDesc.replace(/\\n/g, '<br>');

  modal.classList.add("active");
}

// Đóng popup
document.addEventListener("click", (e) => {
  const modal = document.getElementById("descModal");
  if (e.target === modal || e.target.classList.contains("close-desc")) {
    modal.classList.remove("active");
  }
});

// ================== GIỎ HÀNG & BADGE (giữ nguyên) ==================
function getUserCart() {
  const user = JSON.parse(localStorage.getItem('currentUser') || '{}');
  if (user && user.userId) {
    const saved = localStorage.getItem(`cart_${user.userId}`);
    return saved ? JSON.parse(saved) : [];
  }
  return [];
}

function saveUserCart(cartArray) {
  const user = JSON.parse(localStorage.getItem('currentUser') || '{}');
  if (user && user.userId) {
    localStorage.setItem(`cart_${user.userId}`, JSON.stringify(cartArray));
  }
}

function updateCartBadge() {
  const badge = document.querySelector('.cart-badge');
  if (badge) {
    const cart = getUserCart();
    const total = cart.reduce((sum, item) => sum + item.quantity, 0);
    badge.textContent = total;
    badge.style.display = total > 0 ? 'flex' : 'none';
  }
}

function addToCart(id, name, price, imageUrl) {
  const user = JSON.parse(localStorage.getItem('currentUser') || '{}');
  if (!user.userId) {
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
      checked: true
    });
  }

  saveUserCart(cart);
  alert(`Đã thêm "${name}" vào giỏ hàng!`);
  updateCartBadge();
}

// Khởi chạy
document.addEventListener("DOMContentLoaded", function () {
  loadProducts(currentPage);
  updateCartBadge();
});