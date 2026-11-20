const currentHeader = document.querySelector('#kadr-header');

if (currentHeader) {
    const header = document.createElement('header');
    header.id = 'kadr-header';
    header.className = 'site-header';

    const inner = document.createElement('div');
    inner.className = 'site-header-inner';

    const logo = document.createElement('a');
    logo.className = 'site-header-logo';
    logo.href = '/';
    logo.setAttribute('aria-label', 'КАДР — главная');
    logo.textContent = 'КАДР';

    const navigation = document.createElement('nav');
    navigation.className = 'site-header-nav';
    navigation.setAttribute('aria-label', 'Главное меню');

    const links = [
        { label: 'НАШИ ЗАЛЫ', href: '/#hall' },
        { label: 'ПРАВИЛА', href: '/rules.html', path: '/rules.html' },
        { label: 'КОНТАКТЫ', href: '/#contacts' }
    ];

    for (const item of links) {
        const link = document.createElement('a');
        link.href = item.href;
        link.textContent = item.label;
        if (item.path === window.location.pathname) {
            link.setAttribute('aria-current', 'page');
        }
        navigation.append(link);
    }

    const spacer = document.createElement('span');
    spacer.className = 'site-header-spacer';
    spacer.setAttribute('aria-hidden', 'true');

    inner.append(logo, navigation, spacer);
    header.append(inner);
    currentHeader.replaceWith(header);
}
