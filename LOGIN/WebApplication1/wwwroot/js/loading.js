// Loading screen functionality
document.addEventListener('DOMContentLoaded', function() {
    const loadingOverlay = document.getElementById('loadingOverlay');
    
    // Function to hide loading screen
    function hideLoadingScreen() {
        if (loadingOverlay) {
            loadingOverlay.classList.add('hidden');
            // Remove from DOM after animation completes
            setTimeout(function() {
                if (loadingOverlay) {
                    loadingOverlay.style.display = 'none';
                }
            }, 500);
        }
    }
    
    // Hide loading screen after page loads
    window.addEventListener('load', function() {
        setTimeout(function() {
            hideLoadingScreen();
        }, 800);
    });
    
    // Fallback: hide after 2 seconds regardless (reduced from 3s for faster experience)
    setTimeout(function() {
        hideLoadingScreen();
    }, 2000);
});

// Navbar scroll effect
window.addEventListener('scroll', function() {
    const navbar = document.getElementById('navbar');
    if (navbar) {
        if (window.scrollY > 50) {
            navbar.classList.add('scrolled');
        } else {
            navbar.classList.remove('scrolled');
        }
    }
});