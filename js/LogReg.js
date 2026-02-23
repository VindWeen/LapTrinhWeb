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

// ================= Register =================
document.getElementById('registerForm').addEventListener('submit', async function (e) {
  e.preventDefault();

  const username = document.getElementById('regUsername').value;
  const email = document.getElementById('regEmail').value;
  const password = document.getElementById('regPassword').value;

  try {
    const response = await fetch('http://localhost:5114/api/auth/register', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        username: username,
        password: password,
        role: "Customer"
      })
    });

    if (response.ok) {
      alert('Đăng ký thành công!');
      document.getElementById('registerForm').reset();

      // Trượt về lại login
      container.classList.remove("right-panel-active");
    } else {
      const error = await response.text();
      alert('Đăng ký thất bại: ' + error);
    }
  } catch (err) {
    console.error(err);
    alert('Có lỗi xảy ra! Vui lòng thử lại sau.');
  }
});

// ================= Login =================
document.getElementById('loginForm').addEventListener('submit', async function (e) {
  e.preventDefault();

  const username = document.getElementById('loginUsername').value;
  const password = document.getElementById('loginPassword').value;

  try {
    const response = await fetch('http://localhost:5114/api/auth/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ username, password })
    });

    if (response.ok) {
      const data = await response.json();

      alert(`Đăng nhập thành công! Xin chào ${username}`);

      // Lưu token + thông tin
      localStorage.setItem('token', data.token);
      localStorage.setItem('currentUser', JSON.stringify({
        userID: data.userID,
        role: data.role
      }));

      // Chuyển sang trang loading hoặc index
      window.location.href = 'loadingpage.html';
    } else {
      const error = await response.text();
      alert('Sai thông tin đăng nhập: ' + error);
    }
  } catch (err) {
    console.error(err);
    alert('Có lỗi xảy ra khi đăng nhập.');
  }

  document.getElementById('loginForm').reset();
});