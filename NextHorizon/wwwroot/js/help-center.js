/**
 * Help Center - Main Page Scripts
 * Handles search, category cards, FAQ tabs (NOT chat)
 */

(function() {
    'use strict';

    document.addEventListener('DOMContentLoaded', function() {

        // ================= CATEGORY SEARCH =================
        const searchInput = document.getElementById('categorySearch');
        if (searchInput) {
            searchInput.addEventListener('input', (e) => {
                const term = e.target.value.toLowerCase();
                document.querySelectorAll('.help-card').forEach(card => {
                    const title = card.querySelector('h3')?.innerText.toLowerCase() || '';
                    const desc = card.querySelector('.help-card-desc')?.innerText.toLowerCase() || '';
                    const match = title.includes(term) || desc.includes(term);
                    card.style.display = match ? 'flex' : 'none';
                });
            });
        }

        // ================= FAQ TABS =================
        document.querySelectorAll('.faq-tabs span').forEach(tab => {
            tab.addEventListener('click', () => {
                document.querySelectorAll('.faq-tabs span').forEach(t => t.classList.remove('active'));
                tab.classList.add('active');
                document.querySelectorAll('.faq-list').forEach(list => list.classList.remove('active'));
                const target = document.getElementById(tab.dataset.tab);
                if (target) target.classList.add('active');
            });
        });

        // ================= FAQ ACCORDION =================
        document.querySelectorAll('.faq-question').forEach(q => {
            q.addEventListener('click', () => {
                const item = q.parentElement;
                if (!item) return;
                
                document.querySelectorAll('.faq-item').forEach(i => {
                    if (i !== item) i.classList.remove('active');
                });
                item.classList.toggle('active');
            });
        });

        // ================= EXPAND ALL / COLLAPSE ALL =================
        const toggleAllBtn = document.querySelector('.faq-toggle-all');
        if (toggleAllBtn) {
            toggleAllBtn.addEventListener('click', function() {
                const faqList = document.querySelector('.faq-list.active');
                const items = faqList?.querySelectorAll('.faq-item');
                if (!items || items.length === 0) return;
                
                const isExpanding = !items[0].classList.contains('active');
                items.forEach(item => {
                    item.classList.toggle('active', isExpanding);
                });
                this.textContent = isExpanding ? 'Collapse All' : 'Expand All';
            });
        }

    });
})();
