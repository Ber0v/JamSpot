document.addEventListener('DOMContentLoaded', function () {
    const searchInput = document.getElementById('searchInput');
    const dropdownMenu = document.getElementById('dropdownMenu');

    if (searchInput) {
        searchInput.addEventListener('input', async function () {
            const query = this.value.trim();

            // Ако полето е празно, скрийте менюто
            if (query.length < 1) {
                dropdownMenu.style.display = 'none';
                dropdownMenu.classList.remove('show');
                dropdownMenu.innerHTML = '';
                return;
            }

            try {
                // Заявка към AutoComplete
                const response = await fetch(`/Home/AutoComplete?query=${encodeURIComponent(query)}`);
                if (!response.ok) {
                    throw new Error('Network response was not ok');
                }

                const data = await response.json();
                dropdownMenu.innerHTML = ''; // Изчистете предишното съдържание

                if (data.users.length === 0 && data.groups.length === 0) {
                    // Показване на съобщение, ако няма намерени резултати
                    dropdownMenu.innerHTML = '<div class="dropdown-item text-muted">No users or groups found.</div>';
                } else {
                    // Добавяне на потребители
                    if (data.users.length > 0) {
                        const usersHeader = document.createElement('h6');
                        usersHeader.className = 'dropdown-header';
                        usersHeader.textContent = 'Users';
                        dropdownMenu.appendChild(usersHeader);

                        data.users.forEach(user => {
                            const userItem = document.createElement('a');
                            userItem.href = `/User/All/${user.id}`;
                            userItem.className = 'dropdown-item d-flex align-items-center';
                            userItem.innerHTML = `
                                <img src="${user.avatarUrl || '/images/default-avatar.png'}" alt="${user.userName}" style="width: 30px; height: 30px; border-radius: 50%; margin-right: 10px;">
                                <span>${user.userName}</span>
                            `;
                            dropdownMenu.appendChild(userItem);
                        });
                    }

                    // Добавяне на групи
                    if (data.groups.length > 0) {
                        const groupsHeader = document.createElement('h6');
                        groupsHeader.className = 'dropdown-header';
                        groupsHeader.textContent = 'Groups';
                        dropdownMenu.appendChild(groupsHeader);

                        data.groups.forEach(group => {
                            const groupItem = document.createElement('a');
                            groupItem.href = `/Group/Details/${group.id}`;
                            groupItem.className = 'dropdown-item d-flex align-items-center';
                            groupItem.innerHTML = `
                                <img src="${group.logoUrl || '/images/default-group.png'}" alt="${group.groupName}" style="width: 30px; height: 30px; border-radius: 50%; margin-right: 10px;">
                                <span>${group.groupName}</span>
                            `;
                            dropdownMenu.appendChild(groupItem);
                        });
                    }
                }

                // Показване на менюто
                dropdownMenu.style.display = 'block'; // Активиране на менюто
                dropdownMenu.classList.add('show');
            } catch (error) {
                console.error('Error fetching autocomplete data:', error);
                dropdownMenu.innerHTML = '<div class="dropdown-item text-danger">Error fetching data. Please try again later.</div>';
                dropdownMenu.style.display = 'block'; // Покажете менюто при грешка
                dropdownMenu.classList.add('show');
            }
        });

        // Скриване на менюто при клик извън него
        document.addEventListener('click', function (event) {
            if (!searchInput.contains(event.target) && !dropdownMenu.contains(event.target)) {
                dropdownMenu.style.display = 'none';
                dropdownMenu.classList.remove('show');
                dropdownMenu.innerHTML = '';
            }
        });
    }
});
