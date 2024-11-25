document.addEventListener('DOMContentLoaded', function () {
    var userDropdownButton = document.getElementById('userDropdownButton');
    var userSearchInput = document.getElementById('userSearchInput');
    var userOptions = document.querySelectorAll('.user-option');
    var selectedUserIdInput = document.getElementById('selectedUserId');

    // Function to filter users based on the entered text
    userSearchInput.addEventListener('keyup', function () {
        var filter = userSearchInput.value.toLowerCase();
        userOptions.forEach(function (option) {
            var userName = option.textContent.toLowerCase();
            if (userName.includes(filter)) {
                option.parentElement.style.display = '';
            } else {
                option.parentElement.style.display = 'none';
            }
        });
    });

    // Function to handle user selection
    userOptions.forEach(function (option) {
        option.addEventListener('click', function (e) {
            e.preventDefault();
            var userName = option.textContent.trim();
            var userId = option.getAttribute('data-user-id');

            // Set the selected user's name in the button
            userDropdownButton.querySelector('.selected-user').textContent = userName;

            // Store the selected user's ID in the hidden input field
            selectedUserIdInput.value = userId;

            // Close the dropdown menu
            var dropdown = bootstrap.Dropdown.getInstance(userDropdownButton);
            dropdown.hide();
        });
    });

    // Reset the search input and show all options when opening the dropdown menu
    userDropdownButton.addEventListener('show.bs.dropdown', function () {
        userSearchInput.value = '';
        userOptions.forEach(function (option) {
            option.parentElement.style.display = '';
        });

        // Focus the search input
        setTimeout(function () {
            userSearchInput.focus();
        }, 0);
    });
});
