const searchInput = document.getElementById('searchInput');
const dropdownMenu = document.getElementById('dropdownMenu');

searchInput.addEventListener('input', async () => {
    const query = searchInput.value.trim();

    if (query.length < 2) {
        dropdownMenu.style.display = 'none';
        return;
    }

    console.log('Fetching data for:', query);

    try {
        const response = await fetch(`/Home/AutoComplete?query=${encodeURIComponent(query)}`);
        const data = await response.json();

        console.log('Response:', data);

        let resultsHtml = '';

        // Users
        if (data.users.length > 0) {
            resultsHtml += '<h6 class="dropdown-header">Users</h6>';
            data.users.forEach(user => {
                resultsHtml += `
                        <a href="/Feed/UserDetails/${user.id}" class="dropdown-item">
                            <img src="${user.avatarUrl}" alt="${user.userName}" style="width: 30px; height: 30px; border-radius: 50%; margin-right: 10px;">
                            ${user.userName}
                        </a>`;
            });
        }

        // Groups
        if (data.groups.length > 0) {
            resultsHtml += '<h6 class="dropdown-header">Groups</h6>';
            data.groups.forEach(group => {
                resultsHtml += `
                        <a href="/Group/Details/${group.id}" class="dropdown-item">
                            <img src="${group.logoUrl}" alt="${group.groupName}" style="width: 30px; height: 30px; border-radius: 50%; margin-right: 10px;">
                            ${group.groupName}
                        </a>`;
            });
        }

        dropdownMenu.innerHTML = resultsHtml;
        dropdownMenu.style.display = resultsHtml ? 'block' : 'none';
    } catch (error) {
        console.error('Error fetching autocomplete data:', error);
        dropdownMenu.style.display = 'none';
    }
});

document.addEventListener('click', (event) => {
    if (!dropdownMenu.contains(event.target) && event.target !== searchInput) {
        dropdownMenu.style.display = 'none';
    }
});