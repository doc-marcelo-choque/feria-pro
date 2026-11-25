document.querySelectorAll('form[data-confirm]').forEach(form => form.addEventListener('submit', event => {
    if (!window.confirm(form.dataset.confirm)) event.preventDefault();
}));
document.querySelectorAll('[data-print]').forEach(button => button.addEventListener('click', () => window.print()));
const formulario = document.getElementById('evaluacion');
if (formulario) {
    const recalcular = () => {
        const valores = ['Innovacion', 'DesarrolloTecnico', 'Presentacion'].map(id => document.getElementById(id).value);
        const numeros = valores.map(Number);
        document.getElementById('nota').value = valores.every(x => x !== '') && numeros.every(x => Number.isFinite(x) && x >= 0 && x <= 100)
            ? (numeros[0] * .4 + numeros[1] * .4 + numeros[2] * .2).toFixed(2) : '—';
    };
    formulario.addEventListener('input', recalcular);
    recalcular();
}
