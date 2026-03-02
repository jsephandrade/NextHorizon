/**
 * NEXT HORIZON - Premium Running Gear
 * Enhanced JavaScript with Premium Interactions
 * Version 2.0
 */

const API_URL = '/api';

// State Management
let currentProduct = null;
let selectedSize = null;
let selectedColor = null;
let currentCategory = 'all';
let allProducts = [];
let filteredProducts = [];

// Filter state
let filters = {
    subcategories: [],
    brands: [],
    sizes: [],
    minPrice: 0,
    maxPrice: 10000
};

// =====================================================
// PREMIUM TOAST NOTIFICATION SYSTEM
// =====================================================
class ToastManager {
    constructor() {
        this.container = document.getElementById('toast-container');
    }

    show(message, type = 'default', duration = 3000) {
        const toast = document.createElement('div');
        toast.className = `toast ${type}`;
        toast.innerHTML = `
            <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                ${type === 'success' 
                    ? '<path d="M20 6L9 17l-5-5"></path>' 
                    : '<circle cx="12" cy="12" r="10"></circle><line x1="12" y1="16" x2="12" y2="12"></line><line x1="12" y1="8" x2="12.01" y2="8"></line>'}
            </svg>
            <span>${message}</span>
        `;
        
        this.container.appendChild(toast);
        
        setTimeout(() => {
            toast.classList.add('removing');
            setTimeout(() => toast.remove(), 300);
        }, duration);
    }

    success(message, duration) {
        this.show(message, 'success', duration);
    }

    info(message, duration) {
        this.show(message, 'default', duration);
    }
}

const toast = new ToastManager();

// =====================================================
// HEADER SCROLL EFFECT
// =====================================================
function initHeaderScroll() {
    const header = document.getElementById('main-header');
    let lastScroll = 0;
    
    window.addEventListener('scroll', () => {
        const currentScroll = window.pageYOffset;
        
        if (currentScroll > 50) {
            header.classList.add('scrolled');
        } else {
            header.classList.remove('scrolled');
        }
        
        lastScroll = currentScroll;
    }, { passive: true });
}

// =====================================================
// INITIALIZATION
// =====================================================
document.addEventListener('DOMContentLoaded', () => {
    initHeaderScroll();
    initTopNav();
    // Only load products if the grid exists (Shop page)
    if (document.getElementById('products-grid')) {
        loadProducts('all');
    }
    updateCartCount();
    updateWishlistCount();
    
    // Category filter buttons with enhanced feedback
    document.querySelectorAll('.nav-btn').forEach(btn => {
        btn.addEventListener('click', function() {
            document.querySelectorAll('.nav-btn').forEach(b => b.classList.remove('active'));
            this.classList.add('active');
            const category = this.dataset.category;
            currentCategory = category;
            loadProducts(category);
        });
    });

    // Close modals on escape key
    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape') {
            closeAllModals();
        }
    });

    // Close modals on overlay click
    document.querySelectorAll('.modal').forEach(modal => {
        modal.addEventListener('click', (e) => {
            if (e.target === modal) {
                closeAllModals();
            }
        });
    });
});

// =====================================================
// MAIN NAVIGATION
// =====================================================
function initTopNav() {
    const currentPath = window.location.pathname.toLowerCase();
    const isShop = currentPath.includes('/home/shop');
    const isAbout = currentPath.includes('/home/about');
    const isHome = !isShop && !isAbout && (currentPath === '/' || currentPath === '/home' || currentPath === '/home/index');

    document.querySelectorAll('.nav-link').forEach(link => {
        const href = (link.getAttribute('href') || '').toLowerCase().split('#')[0];
        link.classList.remove('active');
        if (isShop && href.includes('/home/shop')) {
            link.classList.add('active');
        } else if (isAbout && href.includes('/home/about')) {
            link.classList.add('active');
        } else if (isHome && (href === '/' || href === '')) {
            link.classList.add('active');
        }
    });
}

// Navigate to full product page
function openProductModal(productId) {
    window.location.href = `/Home/Product?id=${productId}`;
}

// Alias kept for backwards compat
function showProductDetails(productId) {
    window.location.href = `/Home/Product?id=${productId}`;
}

function scrollToShop() {
    const shopSection = document.querySelector('.shop-layout') || document.querySelector('main');
    if (shopSection) {
        shopSection.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
}

function scrollToAbout() {
    const aboutSection = document.getElementById('about-section');
    if (aboutSection) {
        aboutSection.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
}

function scrollToContact() {
    const contactSection = document.getElementById('contact-section');
    if (contactSection) {
        contactSection.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }
}

// =====================================================
// LOGIN MODAL
// =====================================================
function openLoginModal() {
    document.getElementById('login-modal').classList.add('active');
    document.body.classList.add('modal-open');
}

function closeLoginModal() {
    document.getElementById('login-modal').classList.remove('active');
    document.body.classList.remove('modal-open');
}

function handleLogin(event) {
    event.preventDefault();
    const email = document.getElementById('email').value;
    toast.success(`Welcome back!`);
    closeLoginModal();
}

function toggleRegister() {
    toast.info('Registration coming soon!');
}

function closeAllModals() {
    document.querySelectorAll('.modal').forEach(modal => {
        modal.classList.remove('active');
    });
    document.body.classList.remove('modal-open');
    currentProduct = null;
    selectedSize = null;
    selectedColor = null;
}

// =====================================================
// CURRENCY FORMATTING
// =====================================================
function formatPeso(amount) {
    return `₱${amount.toLocaleString('en-PH', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`;
}

// =====================================================
// PRODUCT LOADING & DISPLAY
// =====================================================
async function loadProducts(category = 'all') {
    try {
        let url = `${API_URL}/products`;
        if (category === 'Men') url = `${API_URL}/products/men`;
        else if (category === 'Women') url = `${API_URL}/products/women`;
        
        const response = await fetch(url);
        const products = await response.json();
        allProducts = products;
        filteredProducts = products;
        
        updateBrandCounts();
        applyFilters();
    } catch (error) {
        console.error('Error loading products:', error);
        toast.info('Unable to load products. Please try again.');
    }
}

function displayProducts(products) {
    const grid = document.getElementById('products-grid');
    const productCount = document.getElementById('product-count');
    if (!grid) return;
    
    if (productCount) productCount.textContent = `(${products.length} product${products.length !== 1 ? 's' : ''})`;
    
    if (products.length === 0) {
        grid.innerHTML = `
            <div style="grid-column: 1 / -1; text-align: center; padding: 64px 24px;">
                <h3 style="font-family: var(--font-display); font-size: 24px; margin-bottom: 12px;">No Products Found</h3>
                <p style="opacity: 0.6;">Try adjusting your filters to find what you're looking for.</p>
            </div>
        `;
        return;
    }
    
    grid.innerHTML = products.map((product, index) => `
        <article class="product-card" onclick="showProductDetails(${product.id})">
            <img src="${product.image}" alt="${product.name}" loading="lazy" onerror="this.src='data:image/svg+xml,%3Csvg xmlns=%27http://www.w3.org/2000/svg%27 width=%27400%27 height=%27400%27%3E%3Crect width=%27400%27 height=%27400%27 fill=%27%23fafafa%27/%3E%3Ctext x=%2750%25%27 y=%2750%25%27 dominant-baseline=%27middle%27 text-anchor=%27middle%27 font-family=%27Inter%27 font-size=%2714%27 fill=%27%23000%27%3ENo Image%3C/text%3E%3C/svg%3E'">
            <div class="product-card-content">
                <h3>${product.name}</h3>
                <div class="price">${formatPeso(product.price)}</div>
                <div class="rating">
                    <span class="stars">${getStars(product.rating)}</span>
                    <span>${product.rating.toFixed(1)}</span>
                </div>
                <div class="product-actions" onclick="event.stopPropagation()">
                    <button class="btn-primary" onclick="quickAddToCart(${product.id})">Add</button>
                </div>
            </div>
        </article>
    `).join('');
}

// =====================================================
// FILTER FUNCTIONS
// =====================================================
function applyFilters() {
    if (!document.getElementById('products-grid')) return;
    
    filters.subcategories = Array.from(document.querySelectorAll('input[name="subcategory"]:checked')).map(cb => cb.value);
    filters.brands = Array.from(document.querySelectorAll('input[name="brand"]:checked')).map(cb => cb.value);
    
    const maxPriceSlider = document.getElementById('price-slider');
    if (maxPriceSlider) filters.maxPrice = parseFloat(maxPriceSlider.value);
    
    let filtered = [...allProducts];
    
    if (filters.subcategories.length > 0) {
        filtered = filtered.filter(p => filters.subcategories.includes(p.subCategory));
    }
    
    if (filters.brands.length > 0) {
        filtered = filtered.filter(p => filters.brands.includes(p.brand));
    }
    
    if (filters.sizes.length > 0) {
        filtered = filtered.filter(p => p.sizes.some(size => filters.sizes.includes(size)));
    }
    
    filtered = filtered.filter(p => p.price <= filters.maxPrice);
    
    filteredProducts = filtered;
    displayProducts(filtered);
}

function clearAllFilters() {
    filters = {
        subcategories: [],
        brands: [],
        sizes: [],
        minPrice: 0,
        maxPrice: 20000
    };
    
    document.querySelectorAll('input[type="checkbox"]').forEach(cb => cb.checked = false);
    document.querySelectorAll('.size-filter-btn').forEach(btn => btn.classList.remove('active'));
    const slider = document.getElementById('price-slider');
    if (slider) { slider.value = 20000; }
    const display = document.getElementById('price-display');
    if (display) { display.textContent = '20,000'; }
    
    applyFilters();
    toast.success('All filters cleared');
}

function toggleSizeFilter(size) {
    const btn = event.target;
    btn.classList.toggle('active');
    
    if (btn.classList.contains('active')) {
        filters.sizes.push(size);
    } else {
        filters.sizes = filters.sizes.filter(s => s !== size);
    }
    
    applyFilters();
}

function updatePriceRange(maxValue) {
    filters.maxPrice = parseInt(maxValue);
    document.getElementById('price-display').textContent = parseInt(maxValue).toLocaleString();
    applyFilters();
}

function updateBrandCounts() {
    const nikeCount = allProducts.filter(p => p.brand === 'Nike').length;
    const adidasCount = allProducts.filter(p => p.brand === 'Adidas').length;
    
    const nikeCountEl = document.getElementById('nike-count');
    const adidasCountEl = document.getElementById('adidas-count');
    
    if (nikeCountEl) nikeCountEl.textContent = nikeCount;
    if (adidasCountEl) adidasCountEl.textContent = adidasCount;
}

function sortProducts(sortBy) {
    let sorted = [...filteredProducts];
    
    switch(sortBy) {
        case 'price-low':
            sorted.sort((a, b) => a.price - b.price);
            break;
        case 'price-high':
            sorted.sort((a, b) => b.price - a.price);
            break;
        case 'rating':
            sorted.sort((a, b) => b.rating - a.rating);
            break;
        case 'newest':
            sorted.sort((a, b) => b.id - a.id);
            break;
        default:
            break;
    }
    
    displayProducts(sorted);
}

// =====================================================
// PRODUCT DETAILS
// =====================================================
function getRelatedProducts(productId, category, limit = 4) {
    return allProducts
        .filter(p => p.id !== productId && p.category === category)
        .slice(0, limit);
}

async function buyNow() {
    if (!selectedSize) {
        toast.info('Please select a size');
        return;
    }

    await addToCart(currentProduct.id, selectedSize);
    closeProductDetails();
    window.location.href = '/Home/Checkout';
}

// =====================================================
// REVIEWS MODAL
// =====================================================
function openReviewsModal() {
    if (!currentProduct || !currentProduct.reviews || currentProduct.reviews.length === 0) {
        return;
    }
    
    const reviewsGrid = document.getElementById('all-reviews-grid');
    reviewsGrid.innerHTML = currentProduct.reviews.map(review => `
        <div class="review-item">
            <div class="review-header">
                <span class="review-name">${review.userName}</span>
                <span class="review-date">${new Date(review.date).toLocaleDateString('en-PH')}</span>
            </div>
            <div class="rating">
                <span class="stars">${getStars(review.rating)}</span>
            </div>
            <p>${review.comment}</p>
        </div>
    `).join('');
    
    document.getElementById('reviews-modal').classList.add('active');
    document.body.classList.add('modal-open');
}

function closeReviewsModal() {
    document.getElementById('reviews-modal').classList.remove('active');
    document.body.classList.remove('modal-open');
}

// =====================================================
// CART FUNCTIONS
// =====================================================
async function quickAddToCart(productId) {
    const product = await fetch(`${API_URL}/products/${productId}`).then(r => r.json());
    const size = product.sizes[0];
    
    await addToCart(productId, size);
}

async function addToCartFromModal() {
    if (!selectedSize) {
        toast.info('Please select a size');
        return;
    }
    
    await addToCart(currentProduct.id, selectedSize);
    closeProductDetails();
}

async function addToCart(productId, size) {
    try {
        const response = await fetch(`${API_URL}/cart`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ productId, size, quantity: 1 })
        });
        
        if (response.ok) {
            updateCartCount();
            toast.success('Added to cart!');
        }
    } catch (error) {
        console.error('Error adding to cart:', error);
        toast.info('Unable to add to cart. Please try again.');
    }
}

async function toggleCart() {
    window.location.href = '/Home/Cart';
}

async function loadCart() {
    try {
        const response = await fetch(`${API_URL}/cart`);
        const cartItems = await response.json();
        
        const cartDiv = document.getElementById('cart-items');
        
        if (cartItems.length === 0) {
            cartDiv.innerHTML = '<p>Your cart is empty</p>';
            document.getElementById('cart-total').textContent = formatPeso(0);
            return;
        }
        
        cartDiv.innerHTML = cartItems.map(item => `
            <div class="cart-item">
                <img src="${item.product.image}" alt="${item.product.name}" loading="lazy" onerror="this.src='data:image/svg+xml,%3Csvg xmlns=%27http://www.w3.org/2000/svg%27 width=%27120%27 height=%27120%27%3E%3Crect width=%27120%27 height=%27120%27 fill=%27%23fafafa%27/%3E%3C/svg%3E'">
                <div class="item-info">
                    <h4>${item.product.name}</h4>
                    <p>Size: ${item.cartItem.size}</p>
                    <div class="quantity-control">
                        <button class="qty-btn" onclick="updateQuantity(${item.product.id}, ${item.cartItem.quantity - 1})">−</button>
                        <span class="qty-value">${item.cartItem.quantity}</span>
                        <button class="qty-btn" onclick="updateQuantity(${item.product.id}, ${item.cartItem.quantity + 1})">+</button>
                    </div>
                    <p class="price">${formatPeso(item.product.price * item.cartItem.quantity)}</p>
                </div>
                <button class="remove-btn" onclick="removeFromCart(${item.product.id})">
                    <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
                        <line x1="18" y1="6" x2="6" y2="18"></line>
                        <line x1="6" y1="6" x2="18" y2="18"></line>
                    </svg>
                </button>
            </div>
        `).join('');
        
        const total = cartItems.reduce((sum, item) => sum + (item.product.price * item.cartItem.quantity), 0);
        document.getElementById('cart-total').textContent = formatPeso(total);
    } catch (error) {
        console.error('Error loading cart:', error);
    }
}

async function removeFromCart(productId) {
    try {
        await fetch(`${API_URL}/cart/${productId}`, { method: 'DELETE' });
        await loadCart();
        updateCartCount();
        toast.success('Removed');
    } catch (error) {
        console.error('Error removing from cart:', error);
    }
}

async function updateQuantity(productId, newQuantity) {
    try {
        if (newQuantity <= 0) {
            await removeFromCart(productId);
            return;
        }
        
        const response = await fetch(`${API_URL}/cart/${productId}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ quantity: newQuantity })
        });
        
        if (response.ok) {
            await loadCart();
            updateCartCount();
        }
    } catch (error) {
        console.error('Error updating quantity:', error);
    }
}

async function updateCartCount() {
    try {
        const response = await fetch(`${API_URL}/cart`);
        const cartItems = await response.json();
        document.getElementById('cart-count').textContent = cartItems.length;
    } catch (error) {
        console.error('Error updating cart count:', error);
    }
}

function checkout() {
    window.location.href = '/Home/Checkout';
}

// =====================================================
// WISHLIST FUNCTIONS
// =====================================================
async function toggleWishlistProduct(productId) {
    await addToWishlist(productId);
}

async function toggleWishlistFromModal() {
    await addToWishlist(currentProduct.id);
}

async function addToWishlist(productId) {
    try {
        const response = await fetch(`${API_URL}/wishlist/${productId}`, { method: 'POST' });
        const result = await response.json();
        updateWishlistCount();
        toast.success(result.message);
    } catch (error) {
        console.error('Error adding to wishlist:', error);
        toast.info('Unable to update wishlist. Please try again.');
    }
}

async function toggleWishlist() {
    window.location.href = '/Home/Wishlist';
}

async function loadWishlist() {
    try {
        const response = await fetch(`${API_URL}/wishlist`);
        const wishlistItems = await response.json();
        
        const wishlistDiv = document.getElementById('wishlist-items');
        
        if (wishlistItems.length === 0) {
            wishlistDiv.innerHTML = '<p>Your wishlist is empty</p>';
            return;
        }
        
        wishlistDiv.innerHTML = wishlistItems.map(item => `
            <div class="wishlist-item">
                <img src="${item.product.image}" alt="${item.product.name}" loading="lazy" onerror="this.src='data:image/svg+xml,%3Csvg xmlns=%27http://www.w3.org/2000/svg%27 width=%27120%27 height=%27120%27%3E%3Crect width=%27120%27 height=%27120%27 fill=%27%23fafafa%27/%3E%3C/svg%3E'">
                <div class="item-info">
                    <h4>${item.product.name}</h4>
                    <p class="price">${formatPeso(item.product.price)}</p>
                    <div class="rating">
                        <span class="stars">${getStars(item.product.rating)}</span>
                        <span>${item.product.rating.toFixed(1)}</span>
                    </div>
                </div>
                <button class="btn-primary" onclick="quickAddToCart(${item.product.id})">Add to Cart</button>
                <button class="remove-btn" onclick="removeFromWishlist(${item.product.id})">Remove</button>
            </div>
        `).join('');
    } catch (error) {
        console.error('Error loading wishlist:', error);
    }
}

async function removeFromWishlist(productId) {
    try {
        await fetch(`${API_URL}/wishlist/${productId}`, { method: 'DELETE' });
        await loadWishlist();
        updateWishlistCount();
        toast.success('Item removed from wishlist');
    } catch (error) {
        console.error('Error removing from wishlist:', error);
    }
}

async function updateWishlistCount() {
    try {
        const response = await fetch(`${API_URL}/wishlist`);
        const wishlistItems = await response.json();
        document.getElementById('wishlist-count').textContent = wishlistItems.length;
    } catch (error) {
        console.error('Error updating wishlist count:', error);
    }
}

// =====================================================
// HELPER FUNCTIONS
// =====================================================
function getStars(rating) {
    const fullStars = Math.floor(rating);
    const halfStar = rating % 1 >= 0.5 ? 1 : 0;
    const emptyStars = 5 - fullStars - halfStar;
    
    return '★'.repeat(fullStars) + (halfStar ? '½' : '') + '☆'.repeat(emptyStars);
}
// =====================================================
// NOTIFICATION BELL
// =====================================================
(function initNotifDot() {
    document.addEventListener('DOMContentLoaded', () => {
        const dot = document.getElementById('notif-dot');
        if (dot && document.querySelectorAll('.notif-item.unread').length > 0) {
            dot.classList.add('active');
        }
        // Close dropdown when clicking outside
        document.addEventListener('click', e => {
            const wrap = document.getElementById('notif-wrap');
            if (wrap && !wrap.contains(e.target)) {
                document.getElementById('notif-dropdown')?.classList.remove('open');
            }
        });
    });
})();

function toggleNotifDropdown() {
    const dd = document.getElementById('notif-dropdown');
    if (!dd) return;
    dd.classList.toggle('open');
    // Mark as read when opened
    if (dd.classList.contains('open')) {
        document.querySelectorAll('.notif-item.unread').forEach(i => i.classList.remove('unread'));
        const dot = document.getElementById('notif-dot');
        if (dot) dot.classList.remove('active');
    }
}

function clearNotifications() {
    const list = document.getElementById('notif-list');
    if (list) {
        list.innerHTML = '<li style="padding:20px 16px;text-align:center;color:#9ca3af;font-size:13px;">No notifications</li>';
    }
    const dot = document.getElementById('notif-dot');
    if (dot) dot.classList.remove('active');
}
// =====================================================
// REVIEW IMAGE SLIDESHOW
// =====================================================
let rvSlideImages = [];
let rvSlideIndex  = 0;

function openReviewSlideshow(images, startIndex) {
    rvSlideImages = images;
    rvSlideIndex  = startIndex || 0;
    document.getElementById('rv-lightbox').classList.add('open');
    document.body.classList.add('sg-lock');
    _updateSlide();
}

function _updateSlide() {
    const img     = document.getElementById('rv-lightbox-img');
    const counter = document.getElementById('rv-slide-counter');
    if (!img) return;

    // Fade out → swap src → fade in
    img.style.opacity = '0';
    setTimeout(() => {
        img.src = rvSlideImages[rvSlideIndex];
        img.style.opacity = '1';
    }, 150);

    if (counter) counter.textContent = rvSlideImages.length > 1
        ? `${rvSlideIndex + 1} / ${rvSlideImages.length}`
        : '';

    // Show arrows only when multiple images
    document.querySelectorAll('.rv-slide-arrow').forEach(btn => {
        btn.style.visibility = rvSlideImages.length > 1 ? 'visible' : 'hidden';
    });
}

function slideshowNav(dir) {
    rvSlideIndex = (rvSlideIndex + dir + rvSlideImages.length) % rvSlideImages.length;
    _updateSlide();
}

function closeReviewSlideshow() {
    document.getElementById('rv-lightbox').classList.remove('open');
    document.body.classList.remove('sg-lock');
    setTimeout(() => {
        const img = document.getElementById('rv-lightbox-img');
        if (img) img.src = '';
    }, 320);
}

document.addEventListener('keydown', function(e) {
    if (!document.getElementById('rv-lightbox')?.classList.contains('open')) return;
    if (e.key === 'Escape')      closeReviewSlideshow();
    if (e.key === 'ArrowRight')  slideshowNav(1);
    if (e.key === 'ArrowLeft')   slideshowNav(-1);
});