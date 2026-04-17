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

    ensureContainer() {
        if (this.container && document.body?.contains(this.container)) {
            return this.container;
        }

        const container = document.createElement('div');
        container.id = 'toast-container';
        container.className = 'toast-container';
        document.body.appendChild(container);
        this.container = container;
        return container;
    }

    show(message, type = 'default', duration = 3000) {
        const container = this.ensureContainer();
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

        container.appendChild(toast);
        
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
window.toast = toast;

// =====================================================
// HEADER SCROLL EFFECT
// =====================================================
function initHeaderScroll() {
    const header = document.getElementById('main-header');
    const promoBar = document.getElementById('promo-bar');
    let lastScroll = 0;
    let ticking = false;

    window.addEventListener('scroll', () => {
        if (!ticking) {
            requestAnimationFrame(() => {
                const currentScroll = window.pageYOffset;
                const promoH = promoBar && !promoBar.classList.contains('hidden')
                    ? promoBar.offsetHeight : 0;

                // Scroll state
                if (currentScroll > 10) {
                    header.classList.add('scrolled');
                } else {
                    header.classList.remove('scrolled');
                }

                // Hide header on fast scroll down, reveal on scroll up
                if (currentScroll > lastScroll && currentScroll > 120) {
                    header.classList.add('header-hidden');
                    document.documentElement.style.setProperty('--products-header-top', '0px');
                } else {
                    header.classList.remove('header-hidden');
                    document.documentElement.style.setProperty('--products-header-top', header.offsetHeight + 'px');
                }

                lastScroll = currentScroll <= 0 ? 0 : currentScroll;
                ticking = false;
            });
            ticking = true;
        }
    }, { passive: true });
}

// =====================================================
// PROMO BAR
// =====================================================
const PROMO_MESSAGES = [
    '\uD83C\uDFC3 Free shipping on orders over \u20B13,000 &nbsp;&middot;&nbsp; New Carbon Racer 2026 dropping <strong>March 15</strong> \u2014 be ready.',
    '\uD83D\uDD25 Sale on select running shoes \u2014 up to <strong>40% off</strong> this week only.',
    '\u2606 Best Sellers restocked \u2014 grab yours before they\u2019re gone.',
    '\uD83D\uDCE6 Orders placed before <strong>5PM</strong> ship same day.',
    '\uD83E\uDD4E New Arrivals just dropped \u2014 <strong>Spring 2026 Collection</strong> is here.',
    'Earn rewards on every purchase \u2014 Join <strong>NEXT HORIZON Members</strong> today.'
];
let _promoIdx = 0;

function initPromoBar() {
    const bar = document.getElementById('promo-bar');
    const header = document.getElementById('main-header');
    // If no promo bar on this page, header sticks at top: 0
    if (!bar) {
        if (header) header.style.top = '0';
        return;
    }
    // Set initial message
    const msgEl = document.getElementById('promo-bar-msg');
    if (msgEl) {
        msgEl.innerHTML = PROMO_MESSAGES[0];
        startPromoRotation(msgEl);
    }
}

function startPromoRotation(msgEl) {
    setInterval(() => {
        msgEl.classList.add('fade-out');
        setTimeout(() => {
            _promoIdx = (_promoIdx + 1) % PROMO_MESSAGES.length;
            msgEl.innerHTML = PROMO_MESSAGES[_promoIdx];
            msgEl.classList.remove('fade-out');
        }, 360);
    }, 5000);
}

// =====================================================
// SEARCH DROPDOWN (Adidas-style)
// =====================================================
let _searchProducts = [];

async function openSearchOverlay() {
    const wrap = document.getElementById('hdr-search-wrap');
    const dropdown = document.getElementById('search-dropdown');
    const panel = dropdown ? dropdown.querySelector('.search-dropdown-panel') : null;
    if (!wrap || !dropdown) return;

    // Position dropdown container flush below the header
    const header = document.getElementById('main-header');
    function _positionSearchPanel() {
        const headerRect = header.getBoundingClientRect();
        const wrapRect = wrap.getBoundingClientRect();
        dropdown.style.top = headerRect.bottom + 'px';
        panel.style.top = '0';
        // Right-align panel with the right edge of the search bar
        // Adjust the offset value (currently 20px) to shift the panel left or right
        const PANEL_OFFSET = 20; // increase = more left, decrease = more right, 0 = flush
        panel.style.right = (window.innerWidth - wrapRect.right - PANEL_OFFSET) + 'px';
        panel.style.left = 'auto';
    }
    if (panel && header) {
        _positionSearchPanel();
    }
    // Close search when user scrolls (panel would drift otherwise)
    const _scrollClose = () => { closeSearchOverlay(); };
    window.addEventListener('scroll', _scrollClose, { passive: true, once: true });

    wrap.classList.add('is-open');
    dropdown.classList.add('active');

    // Preload products for live search
    if (_searchProducts.length === 0) {
        try {
            const res = await fetch(`${API_URL}/products`);
            _searchProducts = await res.json();
        } catch (e) {
            _searchProducts = allProducts.length ? allProducts : [];
        }
    }

    // Default state: show popular, hide results
    const popWrap = document.getElementById('search-popular-wrap');
    const resCols = document.getElementById('search-results-cols');
    if (popWrap) popWrap.style.display = 'block';
    if (resCols) resCols.style.display = 'none';

    setTimeout(() => {
        const input = document.getElementById('search-overlay-input');
        if (input) { input.value = ''; input.focus(); }
    }, 60);
}

function clearOrCloseSearch() {
    const input = document.getElementById('search-overlay-input');
    if (input && input.value.trim()) {
        // Has text — clear it and reset to popular state
        input.value = '';
        handleSearchInput('');
        input.focus();
    } else {
        closeSearchOverlay();
    }
}

function closeSearchOverlay() {
    const wrap = document.getElementById('hdr-search-wrap');
    const dropdown = document.getElementById('search-dropdown');
    if (wrap) wrap.classList.remove('is-open');
    if (dropdown) dropdown.classList.remove('active');
    const input = document.getElementById('search-overlay-input');
    if (input) input.value = '';
}

function handleSearchInput(val) {
    const q = val.trim().toLowerCase();
    const popWrap = document.getElementById('search-popular-wrap');
    const resCols = document.getElementById('search-results-cols');

    if (!q) {
        if (popWrap) popWrap.style.display = 'block';
        if (resCols) resCols.style.display = 'none';
        return;
    }
    if (popWrap) popWrap.style.display = 'none';
    if (resCols) resCols.style.display = 'flex';

    const products = _searchProducts.length ? _searchProducts : allProducts;
    const matched = products.filter(p =>
        (p.name || '').toLowerCase().includes(q) ||
        (p.brand || '').toLowerCase().includes(q) ||
        (p.subCategory || '').toLowerCase().includes(q) ||
        (p.category || '').toLowerCase().includes(q)
    );

    // Build suggestion terms from product data
    const seenTerms = new Set();
    const suggestions = [];
    matched.forEach(p => {
        [p.name, p.subCategory, p.brand].forEach(term => {
            if (!term) return;
            const tl = term.toLowerCase();
            if (tl.includes(q) && !seenTerms.has(tl)) {
                seenTerms.add(tl);
                const count = matched.filter(x =>
                    (x.name || '').toLowerCase().includes(tl) ||
                    (x.brand || '').toLowerCase() === tl ||
                    (x.subCategory || '').toLowerCase() === tl
                ).length;
                suggestions.push({ term, count });
            }
        });
    });

    // Render suggestions
    const sugList = document.getElementById('search-sug-list');
    if (sugList) {
        const rows = suggestions.slice(0, 8).map(s => {
            const hi = s.term.replace(new RegExp(`(${q.replace(/[.*+?^${}()|[\]\\]/g,'\\$&')})`, 'gi'), '<strong>$1</strong>');
            return `<li><a class="search-sug-item" href="/Home/Shop?q=${encodeURIComponent(s.term)}"><span class="search-sug-text">${hi}</span><span class="search-sug-count">${s.count}</span></a></li>`;
        });
        sugList.innerHTML = rows.length ? rows.join('') :
            `<li><a class="search-sug-item" href="/Home/Shop?q=${encodeURIComponent(val)}">Search for &ldquo;${val}&rdquo;</a></li>`;
    }

    // Render products (up to 4)
    const prodList = document.getElementById('search-prod-list');
    if (prodList) {
        const shown = matched.slice(0, 4);
        prodList.innerHTML = shown.length
            ? shown.map(p => `
                <a class="search-prod-item" href="/Home/Product?id=${p.id}">
                    <img class="search-prod-img" src="${p.image}" alt="${p.name}" loading="lazy" onerror="this.src='data:image/svg+xml,%3Csvg xmlns=%22http://www.w3.org/2000/svg%22 width=%2256%22 height=%2256%22%3E%3Crect width=%2256%22 height=%2256%22 fill=%22%23f5f5f5%22/%3E%3C/svg%3E'">
                    <div class="search-prod-info">
                        <div class="search-prod-cat">${p.category} ${p.subCategory}</div>
                        <div class="search-prod-name">${p.name}</div>
                        <div class="search-prod-price">${formatPeso(p.price)}</div>
                    </div>
                </a>`).join('')
            : `<p style="color:#999;font-size:13px;margin:0">No products found</p>`;
    }

    // Update see-all link
    const seeAll = document.getElementById('search-see-all');
    if (seeAll) {
        seeAll.href = `/Home/Shop?q=${encodeURIComponent(val)}`;
        seeAll.textContent = `See all "${val}"`;
    }
}

// =====================================================
// INITIALIZATION
// =====================================================
document.addEventListener('DOMContentLoaded', () => {
    initHeaderScroll();
    initPromoBar();
    initTopNav();
    // Only load products if the grid exists (Shop page)
    if (document.getElementById('products-grid')) {
        loadProducts('all');
    }
    updateCartCount();
    
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
            closeSearchOverlay();
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

function closeAllModals() {
    document.querySelectorAll('.modal').forEach(modal => {
        modal.classList.remove('active');
    });
    closeSearchOverlay();
    closeProfileDropdown();
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
    
    grid.innerHTML = products.map((product, index) => {
        const colorImages = product.colorImages || {};
        const availableColors = product.availableColors || [];
        const swatchesHtml = availableColors.length > 0 ? `
            <div class="card-color-swatches" onclick="event.stopPropagation()">
                ${availableColors.map(color => {
                    const imgs = colorImages[color];
                    const swatchSrc = imgs && imgs.length > 0 ? imgs[0] : null;
                    if (swatchSrc) {
                        return `<button class="card-color-swatch card-swatch-img" title="${color}"
                            onmouseenter="cardSwatchHover(this, '${product.id}', '${swatchSrc.replace(/'/g, "\\'")}', true)"
                            onmouseleave="cardSwatchLeave(this, '${product.id}')"
                            onclick="cardSwatchClick(this, '${product.id}', '${swatchSrc.replace(/'/g, "\\'")}')">
                            <img src="${swatchSrc}" alt="${color}">
                        </button>`;
                    } else {
                        const cssColor = colorNameToCss(color);
                        return `<button class="card-color-swatch card-swatch-dot" title="${color}" style="background:${cssColor};"
                            onmouseenter="cardSwatchHover(this, '${product.id}', null, false)"
                            onmouseleave="cardSwatchLeave(this, '${product.id}')">
                        </button>`;
                    }
                }).join('')}
            </div>` : '';
        return `
        <article class="product-card" id="card-${product.id}" onclick="showProductDetails(${product.id})">
            <div class="product-image">
                <img src="${product.image}" alt="${product.name}" loading="lazy" id="card-img-${product.id}" data-default-src="${product.image}" onerror="this.src='data:image/svg+xml,%3Csvg xmlns=%27http://www.w3.org/2000/svg%27 width=%27400%27 height=%27400%27%3E%3Crect width=%27400%27 height=%27400%27 fill=%27%23fafafa%27/%3E%3Ctext x=%2750%25%27 y=%2750%25%27 dominant-baseline=%27middle%27 text-anchor=%27middle%27 font-family=%27Inter%27 font-size=%2714%27 fill=%27%23000%27%3ENo Image%3C/text%3E%3C/svg%3E'">
                <div class="product-overlay"><span>View Details</span></div>
            </div>
            ${swatchesHtml}
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
        </article>`;
    }).join('');
}

// =====================================================
// CARD COLOR SWATCH HOVER HELPERS
// =====================================================
function cardSwatchHover(btn, productId, imgSrc, isImageSwatch) {
    const img = document.getElementById('card-img-' + productId);
    if (!img) return;
    if (isImageSwatch && imgSrc) {
        img.src = imgSrc;
    }
    btn.parentElement.querySelectorAll('.card-color-swatch').forEach(b => b.classList.remove('card-swatch-active'));
    btn.classList.add('card-swatch-active');
}

function cardSwatchLeave(btn, productId) {
    // Only reset if nothing is permanently selected
    if (!btn.classList.contains('card-swatch-selected')) {
        const img = document.getElementById('card-img-' + productId);
        if (img) {
            const selectedBtn = btn.parentElement.querySelector('.card-swatch-selected');
            if (selectedBtn) {
                // keep selected image
            } else {
                img.src = img.dataset.defaultSrc;
            }
        }
        btn.classList.remove('card-swatch-active');
    }
}

function cardSwatchClick(btn, productId, imgSrc) {
    const img = document.getElementById('card-img-' + productId);
    if (img && imgSrc) img.src = imgSrc;
    // Mark as selected
    btn.parentElement.querySelectorAll('.card-color-swatch').forEach(b => b.classList.remove('card-swatch-selected', 'card-swatch-active'));
    btn.classList.add('card-swatch-selected', 'card-swatch-active');
    // Update default so leave restores to this
    if (img) img.dataset.defaultSrc = imgSrc;
}

const COLOR_MAP = {
    'white': '#ffffff', 'black': '#111111', 'grey': '#9e9e9e', 'gray': '#9e9e9e',
    'navy': '#1a237e', 'red': '#e53935', 'blue': '#1e88e5', 'green': '#43a047',
    'yellow': '#fdd835', 'pink': '#e91e63', 'purple': '#8e24aa', 'orange': '#fb8c00',
    'brown': '#6d4c41', 'charcoal': '#37474f', 'teal': '#00897b', 'gold': '#ffc107',
    'silver': '#bdbdbd', 'crimson': '#c62828', 'volt': '#cddc39', 'carbon': '#455a64',
    'light blue': '#64b5f6', 'lime': '#cddc39'
};
function colorNameToCss(colorName) {
    const key = colorName.toLowerCase().split('/')[0].trim();
    return COLOR_MAP[key] || '#cccccc';
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
        const badge = document.getElementById('cart-count');
        if (badge) {
            const prev = parseInt(badge.textContent) || 0;
            badge.textContent = cartItems.length;
            if (cartItems.length !== prev) {
                badge.classList.remove('pop');
                void badge.offsetWidth; // reflow
                badge.classList.add('pop');
                setTimeout(() => badge.classList.remove('pop'), 400);
            }
        }
    } catch (error) {
        console.error('Error updating cart count:', error);
    }
}

function checkout() {
    window.location.href = '/Home/Checkout';
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

// =====================================================
// FILTER DRAWER (shared across shop pages)
// =====================================================
function openFilterDrawer() {
    const drawer = document.getElementById('filter-drawer');
    const backdrop = document.getElementById('fd-backdrop');
    if (drawer) drawer.classList.add('is-open');
    if (backdrop) backdrop.classList.add('is-open');
    document.body.style.overflow = 'hidden';
}
function closeFilterDrawer() {
    const drawer = document.getElementById('filter-drawer');
    const backdrop = document.getElementById('fd-backdrop');
    if (drawer) drawer.classList.remove('is-open');
    if (backdrop) backdrop.classList.remove('is-open');
    document.body.style.overflow = '';
}
function toggleAccordion(btn) {
    const acc = btn.closest('.fd-accordion');
    const content = acc.querySelector('.fd-acc-content');
    const icon = btn.querySelector('.fd-acc-icon');
    if (acc.classList.contains('is-open')) {
        acc.classList.remove('is-open');
        content.style.display = 'none';
        icon.textContent = '+';
    } else {
        acc.classList.add('is-open');
        content.style.display = '';
        icon.innerHTML = '&#8722;';
    }
}
