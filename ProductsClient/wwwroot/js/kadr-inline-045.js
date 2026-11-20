
document.addEventListener('click', function(event) {
 if (!(event.target instanceof Element)) return;
 var link=event.target.closest('a[href="#hall"]');
 if (!link || event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return;
 var target=document.getElementById('hall');
 if (!target) return;
 event.preventDefault();
 event.stopImmediatePropagation();
 var top=target.getBoundingClientRect().top+window.scrollY-100;
 window.scrollTo({top:Math.max(0,top), behavior:window.matchMedia('(prefers-reduced-motion: reduce)').matches?'auto':'smooth'});
},true);
