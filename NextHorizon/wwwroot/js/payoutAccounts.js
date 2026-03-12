
/* ------------------------------
   SET DEFAULT FUNCTION
--------------------------------*/
function bindDefaultButtons(){

    document.querySelectorAll(".set-default").forEach(function(btn){

        btn.onclick = function(e){

            e.preventDefault();

            const currentDefault = document.querySelector(".badge");

            if(currentDefault){

                const cell = currentDefault.parentElement;

                cell.innerHTML =
                '<a href="#" class="set-default">Set as Default</a>';
            }

            const clickedCell = this.parentElement;

            clickedCell.innerHTML =
            '<span class="badge">Default</span>';

            bindDefaultButtons();
        }

    });

}


/* ------------------------------
   REMOVE ACCOUNT CONFIRMATION
--------------------------------*/
function bindRemoveButtons(){

    document.querySelectorAll(".remove-account").forEach(function(btn){

        btn.onclick = function(e){

            e.preventDefault();

            const confirmDelete = confirm("Are you sure you want to remove this account?");

            if(confirmDelete){

                const row = this.closest("tr");
                row.remove();

            }

        }

    });

}


/* Run functions on page load */
bindDefaultButtons();
bindRemoveButtons();