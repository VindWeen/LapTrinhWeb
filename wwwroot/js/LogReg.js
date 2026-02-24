// LogReg.js - Hoàn chỉnh cho trang đăng nhập/đăng ký

// ================= Hiệu ứng trượt Form =================
const container = document.getElementById('container');
const signUpButton = document.getElementById('signUp');
const signInButton = document.getElementById('signIn');

signUpButton.addEventListener('click', () => {
  container.classList.add("right-panel-active");
});

signInButton.addEventListener('click', () => {
  container.classList.remove("right-panel-active");
});

// ================= ĐĂNG KÝ =================
document.getElementById('registerForm').addEventListener('submit', async function (e) {
  e.preventDefault();

  // Lấy giá trị từ form
  const username     = document.getElementById('regUsername').value.trim();
  const password     = document.getElementById('regPassword').value;
  const fullName     = document.getElementById('regFullName').value.trim();
  const email        = document.getElementById('regEmail').value.trim();
  const dateOfBirth  = document.getElementById('regDateOfBirth').value; // yyyy-mm-dd
  const phone        = document.getElementById('regPhone').value.trim();
  const addressLine  = document.getElementById('regAddressLine').value.trim();
  const province     = document.getElementById('regProvince').value.trim();
  const district     = document.getElementById('regDistrict').value.trim();
  const ward         = document.getElementById('regWard').value.trim();

  // Validation cơ bản (frontend)
  if (!username || !password || !fullName || !email || !dateOfBirth || !phone || !addressLine || !province || !district || !ward) {
    alert('Vui lòng điền đầy đủ thông tin!');
    return;
  }

  if (password.length < 6) {
    alert('Mật khẩu phải ít nhất 6 ký tự!');
    return;
  }

  try {
    const response = await fetch('http://localhost:5000/api/auth/register', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        username: username,
        email: email,
        password: password,
        dateOfBirth: dateOfBirth,
        role: "Customer",           // Mặc định Customer, backend cũng mặc định nếu không gửi
        contactName: fullName,
        contactPhone: phone,
        addressLine: addressLine,
        province: province,
        district: district,
        ward: ward
      })
    });

    if (response.ok) {
      const data = await response.json();
      alert('Đăng ký thành công! UserId: ' + data.userId);

      // Reset form và trượt về đăng nhập
      document.getElementById('registerForm').reset();
      container.classList.remove("right-panel-active");
    } else {
      const errorText = await response.text();
      alert('Đăng ký thất bại: ' + errorText);
    }
  } catch (err) {
    console.error(err);
    alert('Lỗi kết nối server. Vui lòng thử lại sau.');
  }
});

// ================= ĐĂNG NHẬP =================
document.getElementById('loginForm').addEventListener('submit', async function (e) {
  e.preventDefault();

  const username = document.getElementById('loginUsername').value.trim();
  const password = document.getElementById('loginPassword').value;

  if (!username || !password) {
    alert('Vui lòng nhập tên đăng nhập và mật khẩu!');
    return;
  }

  try {
    const response = await fetch('http://localhost:5000/api/auth/login', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ username, password })
    });

    if (response.ok) {
      const data = await response.json();

      alert(`Đăng nhập thành công! Xin chào ${data.username}`);

      // Lưu token và thông tin user
      localStorage.setItem('token', data.token);
      localStorage.setItem('currentUser', JSON.stringify({
        userId: data.userId,
        username: data.username,
        role: data.role
      }));

      // Redirect theo role
      if (data.role === "Admin") {
        window.location.href = 'admin.html';          // Trang admin (tùy bạn)
      } else if (data.role === "Staff") {
        window.location.href = 'staff.html';          // Trang nhân viên
      } else {
        window.location.href = 'index.html';          // Trang khách hàng
      }
    } else {
      const errorText = await response.text();
      alert('Đăng nhập thất bại: ' + errorText);
    }
  } catch (err) {
    console.error(err);
    alert('Lỗi kết nối server khi đăng nhập.');
  }

  // Reset form sau submit
  document.getElementById('loginForm').reset();
});

// Tùy chọn: Nhấn Enter ở input cuối form đăng ký cũng submit
document.getElementById('regWard').addEventListener('keypress', (e) => {
  if (e.key === 'Enter') {
    e.preventDefault();
    document.querySelector('#registerForm button').click();
  }
});