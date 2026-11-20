const dialog = document.getElementById('bookingDialog');
const form = document.getElementById('bookingForm');
const hours = document.getElementById('hours');
const date = document.getElementById('date');
const time = document.getElementById('time');
const notice = document.getElementById('notice');
let selection = null;
const money = value => new Intl.NumberFormat('ru-RU').format(value) + ' ₸';
const today = () => {
    const currentDate = new Date();
    return `${currentDate.getFullYear()}-${String(currentDate.getMonth() + 1).padStart(2, '0')}-${String(currentDate.getDate()).padStart(2, '0')}`;
};

async function api(url, options = {}) {
    let response;
    try {
        response = await fetch(url, {
            headers: { Accept: 'application/json', 'Content-Type': 'application/json' },
            ...options
        });
    } catch (error) {
        if (error instanceof TypeError) {
            throw new Error('Нет связи с приложением. Проверьте, что запущены сайт и сервер API.');
        }
        throw error;
    }

    const responseText = await response.text();
    let result = null;
    if (responseText) {
        try {
            result = JSON.parse(responseText);
        } catch {
            throw new Error('Сервер вернул некорректный ответ.');
        }
    }
    if (!response.ok) {
        const validationError = Object.values(result?.errors ?? {}).flat().find(Boolean);
        throw new Error(result?.detail || validationError || result?.title || 'Не удалось отправить заявку. Попробуйте позже.');
    }
    return result;
}

document.querySelectorAll('button[data-name]').forEach(button => button.addEventListener('click', () => {
    form.reset();
    selection = { room: button.dataset.name, price: Number(button.dataset.price), min: Number(button.dataset.min) };
    document.getElementById('selectedRoom').textContent = selection.room;
    hours.min = selection.min;
    hours.value = selection.min;
    date.min = today();
    hours.setCustomValidity('');
    time.setCustomValidity('');
    date.setCustomValidity('');
    update();
    dialog.showModal();
}));

function update() {
    if (selection) {
        document.getElementById('total').textContent = 'Итого: ' + money(selection.price * Number(hours.value || 0));
    }
}

hours.addEventListener('input', () => { hours.setCustomValidity(''); update(); });
time.addEventListener('input', () => time.setCustomValidity(''));
date.addEventListener('input', () => date.setCustomValidity(''));
document.getElementById('closeDialog').addEventListener('click', () => dialog.close());
form.addEventListener('submit', async event => {
    event.preventDefault();
    if (!selection) return;

    const name = document.getElementById('customer').value.trim();
    if (!name) {
        document.getElementById('customer').focus();
        return;
    }
    const duration = Number(hours.value);
    const start = Number(time.value.split(':')[0]);
    if (!Number.isInteger(duration) || duration < selection.min || start + duration > 22) {
        hours.setCustomValidity('Выберите длительность, чтобы съёмка закончилась не позднее 22:00.');
        hours.reportValidity();
        return;
    }
    const moment = new Date(date.value + 'T' + time.value);
    if (Number.isNaN(moment.getTime()) || moment <= new Date()) {
        date.setCustomValidity('Выберите дату и время в будущем.');
        date.reportValidity();
        return;
    }

    const submit = form.querySelector('button[type="submit"]');
    submit.disabled = true;
    try {
        await api('/api/bookings', {
            method: 'POST',
            body: JSON.stringify({ name, room: selection.room, date: date.value, time: time.value, hours: duration })
        });
        dialog.close();
        notice.textContent = 'Заявка отправлена. Мы свяжемся с вами для подтверждения.';
        notice.scrollIntoView({ behavior: 'smooth' });
    } catch (error) {
        notice.textContent = error.message;
    } finally {
        submit.disabled = false;
    }
});
