// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
$(document).ready(function () {
    const mainNavbar = document.getElementById('mainNavbar');

    // Function to handle navbar style based on scroll position
    const handleScroll = () => {
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

    // Set initial state of the navbar
    if (mainNavbar) {
        handleScroll(); // Run on page load
        window.addEventListener('scroll', handleScroll); // Add scroll event listener
    }
});