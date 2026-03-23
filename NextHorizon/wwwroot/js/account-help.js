// ================= HELP ARTICLE DROPDOWN =================
document.addEventListener('DOMContentLoaded', () => {
  const toggles = document.querySelectorAll('.answer-toggle');

  toggles.forEach((toggle) => {
    toggle.addEventListener('click', (e) => {
      e.preventDefault();

      const parent = toggle.closest('.help-article');

      // OPTIONAL: close others (Shopee-style behavior)
      document.querySelectorAll('.help-article').forEach((item) => {
        if (item !== parent) {
          item.classList.remove('open');
        }
      });

      // Toggle current
      parent.classList.toggle('open');
    });
  });
});
