t_onReady(function() {
            
            var selects='button:not(.t-submit):not(.t835__btn_next):not(.t835__btn_prev):not(.t835__btn_result):not(.t862__btn_next):not(.t862__btn_prev):not(.t862__btn_result):not(.t854__news-btn):not(.t862__btn_next),' +
'a:not([href*="#"]):not(.carousel-control):not(.t-carousel__control):not(.t807__btn_reply):not([href^="#price"]):not([href^="javascript"]):not([href^="mailto"]):not([href^="tel"]):not([href^="link_sub"]):not(.js-feed-btn-show-more):not(.t367__opener):not([href^="https://www.dropbox.com/"])';
var elements=document.querySelectorAll(selects);Array.prototype.forEach.call(elements,function(element) {
    
    var submenuLinkWrapper=element.closest('.t-menu__link-wrapper, .t-menusub__link-wrapper');
    
    var isSubmenu=element.getAttribute('data-menu-submenu-hook')||(submenuLinkWrapper&&submenuLinkWrapper.getAttribute('data-menu-submenu-hook'));

    if(isSubmenu) return;element.addEventListener('click',function(event) {
        var goTo=this.getAttribute('href');if(goTo!==null&&!goTo.startsWith('#')) {
            var ctrl=event.ctrlKey;
        var cmd=event.metaKey&&navigator.platform.indexOf('Mac')!==-1;if(!ctrl&&!cmd) {
            var target=this.getAttribute('target');
    if(target!=='_blank') {
        event.preventDefault();
        var allRecords=document.querySelector('.t-records');
        if(allRecords) {
            allRecords.classList.remove('t-records_visible');}
setTimeout(function() {window.location=goTo;},500);}}}});});});
