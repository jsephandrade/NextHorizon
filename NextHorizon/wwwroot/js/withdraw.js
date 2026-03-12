document.addEventListener("DOMContentLoaded", function () {

    const maxBtn = document.querySelector(".max-btn");
    const amountInput = document.querySelector(".amount-input input");
    const balanceDisplay = document.querySelector(".balance-display");

    if (maxBtn && amountInput && balanceDisplay) {

        maxBtn.addEventListener("click", function () {

            // Get balance value
            let balanceText = balanceDisplay.textContent;

            // Remove peso sign and commas
            let balance = balanceText.replace("₱", "").replace(/,/g, "").trim();

            // Put value into input
            amountInput.value = balance;

        });

    }

});