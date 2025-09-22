// Function to handle all page-load events and interactions
document.addEventListener("DOMContentLoaded", function () {

    // --- Navbar Scroll Logic ---
    const mainNavbar = document.getElementById('mainNavbar');
    if (mainNavbar) {
        const handleNavbarScroll = () => {
            if (window.scrollY > 50) {
                mainNavbar.classList.add('navbar-scrolled');
                mainNavbar.classList.remove('navbar-transparent');
            } else {
                mainNavbar.classList.add('navbar-transparent');
                mainNavbar.classList.remove('navbar-scrolled');
            }
        };
        handleNavbarScroll();
        window.addEventListener('scroll', handleNavbarScroll);
    }

    // --- Back to Top Button Logic ---
    const backToTopBtn = document.getElementById('backToTopBtn');
    if (backToTopBtn) {
        window.addEventListener('scroll', () => {
            if (window.scrollY > 300) {
                backToTopBtn.classList.remove('d-none');
            } else {
                backToTopBtn.classList.add('d-none');
            }
        });
        backToTopBtn.addEventListener('click', (e) => {
            e.preventDefault();
            window.scrollTo({
                top: 0,
                behavior: 'smooth'
            });
        });
    }

    // --- Show/Hide Password Toggle Logic ---
    const togglePasswordButtons = document.querySelectorAll('.toggle-password-button');
    togglePasswordButtons.forEach(button => {
        button.addEventListener('click', function () {
            const inputGroup = this.closest('.input-group');
            const passwordInput = inputGroup.querySelector('input');
            const icon = this.querySelector('i');

            if (passwordInput.type === 'password') {
                passwordInput.type = 'text';
                icon.classList.remove('bi-eye-fill');
                icon.classList.add('bi-eye-slash-fill');
            } else {
                passwordInput.type = 'password';
                icon.classList.remove('bi-eye-slash-fill');
                icon.classList.add('bi-eye-fill');
            }
        });
    });

}); // This is the single closing tag for the main event listener