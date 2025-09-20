  /* this Js code for hide the side bar symbal in Admin side - sidebar panel. After click the symbal side bar open and the symbal will be hidden.  */
    document.addEventListener('DOMContentLoaded', function () {
            const adminSidebar = document.getElementById('adminSidebar');
    const sidebarToggle = document.querySelector('.sidebar-toggle');

    if (adminSidebar && sidebarToggle) {
        // When the sidebar starts to open, add the 'is-hidden' class to the button
        adminSidebar.addEventListener('show.bs.offcanvas', () => {
            sidebarToggle.classList.add('is-hidden');
        });

                // When the sidebar is fully closed, remove the 'is-hidden' class
                adminSidebar.addEventListener('hidden.bs.offcanvas', () => {
        sidebarToggle.classList.remove('is-hidden');
                });
            }
        });
