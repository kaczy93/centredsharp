// Page router - loads content dynamically
const pages = {
    'home': 'pages/home.html',
    'download': 'pages/download.html',
    'donate': 'pages/donate.html'
};

// Load page content
async function loadPage(pageName) {
    const contentContainer = document.getElementById('content-container');
    const pageUrl = pages[pageName] || pages['home'];
    
    try {
        const response = await fetch(pageUrl);
        if (!response.ok) throw new Error('Page not found');
        
        const html = await response.text();
        contentContainer.innerHTML = html;
        
        // Update active nav link
        updateActiveNav(pageName);
        
        // Update page title
        updatePageTitle(pageName);
        
        // Re-attach event listeners for dynamically loaded content
        attachDynamicEventListeners();
        
    } catch (error) {
        console.error('Error loading page:', error);
        contentContainer.innerHTML = '<p class="text-red-600">Error loading content. Please try again.</p>';
    }
}

// Update active navigation link styling
function updateActiveNav(pageName) {
    const navLinks = document.querySelectorAll('.nav-link');
    navLinks.forEach(link => {
        const linkPage = link.getAttribute('data-page');
        if (linkPage === pageName) {
            link.classList.remove('text-brown', 'hover-text-gray-400');
            link.classList.add('text-teal-500', 'hover-text-teal-400');
        } else {
            link.classList.remove('text-teal-500', 'hover-text-teal-400');
            link.classList.add('text-brown', 'hover-text-gray-400');
        }
    });
}

// Update page title
function updatePageTitle(pageName) {
    const titles = {
        'home': 'CentrED#',
        'download': 'Download - CentrED#',
        'donate': 'Donate - CentrED#'
    };
    document.title = titles[pageName] || titles['home'];
}

// Attach event listeners for dynamically loaded links
function attachDynamicEventListeners() {
    const dynamicLinks = document.querySelectorAll('[data-page]');
    dynamicLinks.forEach(link => {
        link.addEventListener('click', function(e) {
            const pageName = this.getAttribute('data-page');
            if (pageName) {
                e.preventDefault();
                window.location.hash = pageName;
            }
        });
    });
}

// Handle hash changes (back/forward buttons)
function handleHashChange() {
    let hash = window.location.hash.substring(1) || 'home';
    loadPage(hash);
}

// Mobile menu toggle
function setupMobileMenu() {
    const navbarToggle = document.getElementById('navbar-toggle');
    const navbarMenu = document.querySelector('.navbar-menu');
    const menuOpen = document.getElementById('menu-open');
    const menuClose = document.getElementById('menu-close');

    if (navbarToggle && navbarMenu) {
        navbarToggle.addEventListener('click', function() {
            navbarMenu.classList.toggle('active');
            menuOpen.classList.toggle('hidden');
            menuClose.classList.toggle('hidden');
        });
        
        // Close menu when clicking nav links
        const navLinks = navbarMenu.querySelectorAll('a');
        navLinks.forEach(link => {
            link.addEventListener('click', function() {
                navbarMenu.classList.remove('active');
                menuOpen.classList.remove('hidden');
                menuClose.classList.add('hidden');
            });
        });
    }
}

// Initialize on page load
document.addEventListener('DOMContentLoaded', function() {
    setupMobileMenu();
    handleHashChange();
    attachDynamicEventListeners();
    
    // Listen for hash changes (browser back/forward)
    window.addEventListener('hashchange', handleHashChange);
});
