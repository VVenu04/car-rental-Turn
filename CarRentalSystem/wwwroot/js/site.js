// Function to handle all page-load events and interactions
document.addEventListener("DOMContentLoaded", function () {
    // --- Navbar Scroll Logic ---
    const mainNavbar = document.getElementById('mainNavbar');

    if (mainNavbar) {
        const handleNavbarScroll = () => {
            if (window.scrollY > 50) {
                // Add scrolled class and remove transparent class
                mainNavbar.classList.add('navbar-scrolled');
                mainNavbar.classList.remove('navbar-transparent');
            } else {
                // Add transparent class and remove scrolled class
                mainNavbar.classList.add('navbar-transparent');
                mainNavbar.classList.remove('navbar-scrolled');
            }
        };

        handleNavbarScroll(); // Set initial state on page load
        window.addEventListener('scroll', handleNavbarScroll); // Listen for scroll events
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
});