const container = document.getElementById('adminRequests');
const notice = document.getElementById('adminNotice');
const antiforgeryToken = document.querySelector('#antiforgeryForm input[name="__RequestVerificationToken"]').value;
const money = value => new Intl.NumberFormat('ru-RU').format(value) + ' ₸';

async function request(url, options = {}) {
    const response = await fetch(url, {
        ...options,
        headers: { Accept: 'application/json', 'RequestVerificationToken': antiforgeryToken, ...options.headers }
    });
    const body = await response.text();
    let result = null;
    if (body) {
        try {
            result = JSON.parse(body);
        } catch {
            throw new Error('Сервер вернул некорректный ответ.');
        }
    }
    if (response.status === 401 || response.status === 403) {
        window.location.assign('/admin/login');
        throw new Error('Сеанс завершён. Войдите снова.');
    }
    if (!response.ok) {
        throw new Error(result?.detail || result?.title || 'Не удалось выполнить запрос.');
    }
    return result;
}

async function loadBookings() {
    container.textContent = 'Загрузка заявок…';
    notice.textContent = '';
    try {
        const bookings = await request('/api/bookings');
        container.replaceChildren();
        if (!Array.isArray(bookings)) {
            throw new Error('Сервер вернул список заявок в неожиданном формате.');
        }
        if (bookings.length === 0) {
            container.textContent = 'Заявок пока нет.';
            return;
        }

        for (const booking of bookings) {
            const row = document.createElement('article');
            row.className = 'request';
            const details = document.createElement('div');
            const title = document.createElement('strong');
            title.textContent = `${booking.room} · ${money(booking.total)}`;
            const summary = document.createElement('p');
            summary.textContent = `${booking.name} · ${booking.date} · ${booking.time} · ${booking.hours} ч.`;
            details.append(title, summary);
            const remove = document.createElement('button');
            remove.type = 'button';
            remove.textContent = 'Удалить';
            remove.addEventListener('click', async () => {
                if (!window.confirm(`Удалить заявку «${booking.room}» от ${booking.name}?`)) return;
                remove.disabled = true;
                try {
                    await request(`/api/bookings/${booking.id}`, { method: 'DELETE' });
                    await loadBookings();
                    notice.textContent = 'Заявка удалена.';
                } catch (error) {
                    notice.textContent = error.message;
                    remove.disabled = false;
                }
            });
            row.append(details, remove);
            container.append(row);
        }
    } catch (error) {
        container.textContent = 'Не удалось загрузить заявки.';
        notice.textContent = error.message;
    }
}

loadBookings();
