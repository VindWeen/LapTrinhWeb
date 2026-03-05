// Cart.js - Hoàn chỉnh, lấy giỏ hàng từ SQL qua API /api/cart

// ==================== HÀM LẤY GIỎ HÀNG TỪ SERVER ====================
async function getUserCart() {
  const token = localStorage.getItem('token');
  if (!token) return [];

  try {
    const res = await fetch('http://localhost:5000/api/cart', {
      headers: { 'Authorization': `Bearer ${token}` }
    });

    if (!res.ok) {
      console.error('Lỗi lấy giỏ hàng:', res.statusText);
      return [];
    }

    const result = await res.json();
    return result.success ? result.data : [];
  } catch (err) {
    console.error('Lỗi kết nối API giỏ hàng:', err);
    return [];
  }
}

// ==================== CẬP NHẬT BADGE GIỎ HÀNG TỪ SERVER ====================
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

// ==================== TẢI THÔNG TIN USER TỰ ĐỘNG ====================
async function loadUserInfo() {
  const token = localStorage.getItem('token');
  if (!token) return;

  try {
    const res = await fetch('http://localhost:5000/api/users/me', {
      headers: { 'Authorization': `Bearer ${token}` }
    });

    if (res.ok) {
      const userData = await res.json();
      document.getElementById('fullName').value = userData.contactName || userData.fullName || '';
      document.getElementById('phone').value = userData.contactPhone || userData.phone || '';
      document.getElementById('email').value = userData.email || '';
      document.getElementById('address').value = userData.addressLine || '';
    }
  } catch (err) {
    console.error('Lỗi tải thông tin user:', err);
  }
}

// ==================== RENDER GIỎ HÀNG TỪ SERVER ====================
async function renderCart() {
  const cartItemsEl = document.getElementById('cartItems');
  const subtotalEl = document.getElementById('subtotal');
  const totalEl = document.getElementById('total');
  const payBtn = document.getElementById('payBtn');

  cartItemsEl.innerHTML = '<p>Đang tải giỏ hàng...</p>';

  const cart = await getUserCart();

  cartItemsEl.innerHTML = '';

  if (!cart || cart.length === 0) {
    cartItemsEl.innerHTML = `
      <div class="empty-cart">
        <div class="empty-icon">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <circle cx="9" cy="21" r="1" />
            <circle cx="20" cy="21" r="1" />
            <path d="M1 1h4l2.68 13.39a2 2 0 0 0 2 1.61h9.72a2 2 0 0 0 2-1.61L23 6H6" />
          </svg>
        </div>
        <h2>Giỏ hàng của bạn đang trống</h2>
        <p>Hãy thêm sản phẩm yêu thích để tiếp tục mua sắm nhé!</p>
        <button class="btn-shop-now" onclick="window.location.href='Product.html'">Tiếp tục mua sắm</button>
      </div>
    `;
    payBtn.disabled = true;
    subtotalEl.textContent = '0 ₫';
    totalEl.textContent = '30.000 ₫';
    return;
  }

  let subtotal = 0;
  const shippingFee = 30000;

  cart.forEach(item => {
    // Tính tổng tiền item
    const price = item.product?.price || 0;
    const itemTotal = price * item.quantity;
    subtotal += itemTotal;

    // Tạo text biến thể đẹp kiểu Shopee (không hiện "Biến thể:" nữa)
    let variantTag = '';
    if (item.variant) {
      const parts = [];
      if (item.variant.colorName) parts.push(item.variant.colorName);
      if (item.variant.sizeName) parts.push(item.variant.sizeName);
      if (item.variant.sku && parts.length === 0) parts.push(`SKU: ${item.variant.sku}`);

      if (parts.length > 0) {
        variantTag = `<span class="variant-tag">${parts.join(' - ')}</span>`;
      }
    }

    const div = document.createElement('div');
    div.className = 'cart-item';
    div.innerHTML = `
      <input type="checkbox" class="cart-checkbox" data-id="${item.id}" ${item.checked !== false ? 'checked' : ''} />
      <img src="${item.product?.thumbnail ? 'http://localhost:5000/Images/' + item.product.thumbnail : 'https://via.placeholder.com/80?text=No+Image'}" 
           alt="${item.product?.name || 'Sản phẩm'}" 
           onerror="this.src='https://placehold.co/80x80?text=Error'" />
      <div class="item-info">
        <h3>${item.product?.name || 'Sản phẩm không xác định'}</h3>
        <div class="price">${price.toLocaleString('vi-VN')} ₫</div>
        ${variantTag}  <!-- Khung nhỏ mờ hiển thị biến thể (Trắng - M, Đen - S, ...) -->
      </div>
      <div class="quantity-controls">
        <button onclick="changeQuantity(${item.id}, -1)">-</button>
        <span>${item.quantity}</span>
        <button onclick="changeQuantity(${item.id}, 1)">+</button>
      </div>
      <button class="remove-item" onclick="removeItem(${item.id})">×</button>
    `;
    cartItemsEl.appendChild(div);
  });

  const total = subtotal + shippingFee;
  subtotalEl.textContent = subtotal.toLocaleString('vi-VN') + ' ₫';
  totalEl.textContent = total.toLocaleString('vi-VN') + ' ₫';

  // Nút đặt hàng chỉ sáng khi có item checked
  payBtn.disabled = !cart.some(item => item.checked !== false);
}

// ==================== THAY ĐỔI SỐ LƯỢNG (gọi PUT /api/cart/{id}) ====================
async function changeQuantity(itemId, delta) {
  const token = localStorage.getItem('token');
  if (!token) return;

  try {
    const cart = await getUserCart();
    const item = cart.find(i => i.id === itemId);
    if (!item) return;

    const newQuantity = Math.max(1, item.quantity + delta);

    const res = await fetch(`http://localhost:5000/api/cart/${itemId}`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
      },
      body: JSON.stringify({ quantity: newQuantity })
    });

    const result = await res.json();

    if (res.ok && result.success) {
      renderCart();
      updateCartBadge();
    } else {
      alert(result.message || 'Lỗi cập nhật số lượng');
    }
  } catch (err) {
    console.error('Lỗi thay đổi số lượng:', err);
    alert('Lỗi kết nối server');
  }
}

// ==================== XÓA ITEM (gọi DELETE /api/cart/{id}) ====================
async function removeItem(itemId) {
  if (!confirm('Bạn có chắc muốn xóa sản phẩm này khỏi giỏ hàng?')) return;

  const token = localStorage.getItem('token');
  if (!token) return;

  try {
    const res = await fetch(`http://localhost:5000/api/cart/${itemId}`, {
      method: 'DELETE',
      headers: { 'Authorization': `Bearer ${token}` }
    });

    const result = await res.json();

    if (res.ok && result.success) {
      renderCart();
      updateCartBadge();
    } else {
      alert(result.message || 'Lỗi xóa sản phẩm');
    }
  } catch (err) {
    console.error('Lỗi xóa item:', err);
    alert('Lỗi kết nối server');
  }
}

// ==================== TOGGLE CHECKBOX (lưu tạm ở frontend) ====================
function toggleChecked(itemId, isChecked) {
  let checkedItems = JSON.parse(localStorage.getItem('checkedCartItems') || '{}');
  checkedItems[itemId] = isChecked;
  localStorage.setItem('checkedCartItems', JSON.stringify(checkedItems));

  renderCart();
}

// ==================== KIỂM TRA FORM ĐẦY ĐỦ ====================
function checkFormComplete() {
  const fullName = document.getElementById('fullName').value.trim();
  const phone = document.getElementById('phone').value.trim();
  const address = document.getElementById('address').value.trim();

  const checkedItems = JSON.parse(localStorage.getItem('checkedCartItems') || '{}');
  const hasChecked = Object.values(checkedItems).some(v => v);

  document.getElementById('payBtn').disabled = !(fullName && phone && address && hasChecked);
}

// ==================== ĐẶT HÀNG (gửi lên /api/orders) ====================
async function placeOrder() {
  const token = localStorage.getItem('token');
  if (!token) {
    alert('Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại!');
    window.location.href = 'LogReg.html';
    return;
  }

  const cart = await getUserCart();
  if (cart.length === 0) {
    alert('Giỏ hàng trống!');
    return;
  }

  const fullName = document.getElementById('fullName').value.trim();
  const phone = document.getElementById('phone').value.trim();
  const address = document.getElementById('address').value.trim();

  if (!fullName || !phone || !address) {
    alert('Vui lòng điền đầy đủ thông tin giao hàng!');
    return;
  }

  const checkedItems = JSON.parse(localStorage.getItem('checkedCartItems') || '{}');
  const selectedItems = cart.filter(item => checkedItems[item.id]);

  if (selectedItems.length === 0) {
    alert('Vui lòng chọn ít nhất một sản phẩm để đặt hàng!');
    return;
  }

  const orderData = {
    ShippingName: fullName,
    ShippingAddress: address,
    ShippingPhone: phone,
    PaymentMethod: "COD",
    OrderDetails: selectedItems.map(item => ({
      SnapshotProductName: item.product?.name || "Sản phẩm",
      SnapshotSKU: item.variant?.sku || "N/A",
      Quantity: item.quantity,
      UnitPrice: item.product?.price || 0
    }))
  };

  try {
    const res = await fetch('http://localhost:5000/api/orders', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
      },
      body: JSON.stringify(orderData)
    });

    const result = await res.json();

    if (res.ok && result.success) {
      alert(`Đặt hàng thành công!\nMã đơn: ${result.data?.orderCode || 'Đang xử lý'}`);

      for (const item of selectedItems) {
        await fetch(`http://localhost:5000/api/cart/${item.id}`, {
          method: 'DELETE',
          headers: { 'Authorization': `Bearer ${token}` }
        });
      }

      localStorage.removeItem('checkedCartItems');

      renderCart();
      updateCartBadge();

      window.location.href = 'Orders.html';
    } else {
      alert('Đặt hàng thất bại: ' + (result.message || 'Lỗi server'));
    }
  } catch (err) {
    console.error('Lỗi đặt hàng:', err);
    alert('Lỗi kết nối server');
  }
}

// ==================== KHỞI CHẠY ====================
document.addEventListener('DOMContentLoaded', async () => {
  await loadUserInfo();
  await renderCart();
  await updateCartBadge();

  ['fullName', 'phone', 'address'].forEach(id => {
    const el = document.getElementById(id);
    if (el) el.addEventListener('input', checkFormComplete);
  });

  const payBtn = document.getElementById('payBtn');
  if (payBtn) payBtn.addEventListener('click', placeOrder);

  document.addEventListener('change', e => {
    if (e.target.classList.contains('cart-checkbox')) {
      const itemId = parseInt(e.target.dataset.id);
      toggleChecked(itemId, e.target.checked);
    }
  });
});