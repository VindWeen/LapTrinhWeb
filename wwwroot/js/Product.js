// ============= Load sản phẩm từ API + Popup mô tả + Phân trang =============
let currentPage = 1;
const pageSize = 12;

async function loadProducts(page = 1) {
  const list = document.getElementById("product-list");
  const pagination = document.getElementById("pagination");
  list.innerHTML = "<h3>Đang tải sản phẩm...</h3>";
  pagination.innerHTML = "";

  try {
    // Sắp xếp tăng dần theo ID (cũ nhất ở trên)
    // Nếu muốn giảm dần: thêm &orderBy=desc (sau này thêm param nếu cần)
    const url = `http://localhost:5000/api/admin/products?page=${page}&pageSize=${pageSize}&search=`;
    
    const res = await fetch(url);
    const result = await res.json();

    if (!result.success || !result.data) {
      list.innerHTML = "<h3>Không tải được sản phẩm.</h3>";
      return;
    }

    const products = result.data;
    const totalRecords = result.pagination.totalRecords;
    const totalPages = result.pagination.totalPages;

    list.innerHTML = "";

    products.forEach(p => {
      const imageUrl = p.thumbnail 
        ? `http://localhost:5000/Images/${p.thumbnail}` 
        : "https://via.placeholder.com/300x240?text=No+Image";

      const formattedPrice = (p.price || 0).toLocaleString('vi-VN');

      // Rating: chỉ 1 sao vàng viền đen + điểm số
      const rating = p.averageRating || 0;
      const ratingHTML = `<span class="star filled">★</span> <span class="rating-text">${rating.toFixed(1)}</span>`;

      // Color dots
      let colorsHTML = '';
      if (p.colors && p.colors.length > 0) {
        colorsHTML = p.colors.map(c => `
          <div class="color-dot" style="background-color: ${c.hexCode}" title="${c.name}"></div>
        `).join('');
      }

      // Size range: nếu chỉ 1 size thì không hiện dấu "-"
      let sizeRangeText = '';
      if (p.sizeRange) {
        const sizes = p.sizeRange.split(' - ');
        sizeRangeText = sizes.length > 1 ? p.sizeRange : sizes[0];
      }

      const card = document.createElement("div");
      card.className = "card";
      card.style.cursor = "pointer";

      card.onclick = () => {
        window.location.href = `ProductDetail.html?id=${p.id}`;
      };

      card.innerHTML = `
        <img src="${imageUrl}" alt="${p.name}" onerror="this.src='https://via.placeholder.com/300x240?text=Error';" />
        <div class="card-info">
          <h3>${p.name}</h3>
          <div class="rating-line">
            <div class="rating">${ratingHTML}</div>
            <span class="size-range">${sizeRangeText}</span>
          </div>
          <div class="color-dots">${colorsHTML}</div>
          <div class="price">${formattedPrice} đ</div>
          <div class="desc">
            ${p.description && p.description.length > 100 
              ? p.description.slice(0, 100) + "..." 
              : p.description || "Không có mô tả"}
            ${p.description && p.description.length > 100 
              ? `<span class="detail-btn" onclick="event.stopPropagation(); showDescription(${p.id}, '${p.name.replace(/'/g, "\\'")}', \`${(p.description || '').replace(/`/g, "\\`")}\`)">Xem chi tiết</span>`
              : ""}
          </div>
          <div class="stock">Tồn kho: <span style="color:#10b981">Có sẵn</span></div>
        </div>
      `;
      list.appendChild(card);
    });

    // Render phân trang
    renderPagination(page, totalPages);
  } catch (err) {
    console.error(err);
    list.innerHTML = "<h3>Lỗi kết nối server.</h3>";
  }
}

// Hàm render nút phân trang (1 2 3...)
function renderPagination(current, totalPages) {
  const pagination = document.getElementById("pagination");
  if (!pagination || totalPages <= 1) return;

  let html = '';

  // Nút Previous
  html += `<button ${current === 1 ? 'disabled' : ''} onclick="loadProducts(${current - 1})">Trước</button>`;

  // Hiển thị 5 trang gần nhất (có thể điều chỉnh)
  const startPage = Math.max(1, current - 2);
  const endPage = Math.min(totalPages, current + 2);

  for (let i = startPage; i <= endPage; i++) {
    html += `<button class="${i === current ? 'active' : ''}" onclick="loadProducts(${i})">${i}</button>`;
  }

  // Nút Next
  html += `<button ${current === totalPages ? 'disabled' : ''} onclick="loadProducts(${current + 1})">Sau</button>`;

  pagination.innerHTML = html;
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

// Cập nhật badge giỏ hàng từ server
async function updateCartBadge() {
  const badge = document.querySelector('.cart-badge');
  if (!badge) return;

  const token = localStorage.getItem('token');
  if (!token) {
    badge.style.display = 'none';
    return;
  }

  try {
    const res = await fetch('http://localhost:5000/api/cart', {
      headers: { 'Authorization': `Bearer ${token}` }
    });

    if (res.ok) {
      const result = await res.json();
      if (result.success) {
        const total = result.data.reduce((sum, item) => sum + item.quantity, 0);
        badge.textContent = total;
        badge.style.display = total > 0 ? 'flex' : 'none';
      } else {
        badge.style.display = 'none';
      }
    }
  } catch (err) {
    console.error('Lỗi cập nhật badge:', err);
    badge.style.display = 'none';
  }
}

// Khởi chạy
document.addEventListener("DOMContentLoaded", function () {
  loadProducts(currentPage);
  updateCartBadge();
});