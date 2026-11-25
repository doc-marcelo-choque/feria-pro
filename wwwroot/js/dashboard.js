(() => {
    const datos = document.getElementById('dashboard-datos');
    if (!datos || !window.Chart) return;
    const d = JSON.parse(datos.textContent);
    const crear = (id, tipo, etiquetas, valores) => new Chart(document.getElementById(id), {
        type: tipo,
        data: { labels: etiquetas, datasets: [{ label: 'Proyectos', data: valores, backgroundColor: ['#153d64', '#377d9d', '#55a58a', '#92b4c8', '#657789'] }] },
        options: { responsive: true, maintainAspectRatio: false, plugins: { legend: { display: tipo !== 'bar' } }, ...(tipo === 'bar' ? { scales: { y: { beginAtZero: true, ticks: { precision: 0 } } } } : {}) }
    });
    crear('por-feria', 'bar', d.Ferias, d.ProyectosPorFeria);
    crear('por-estado', 'doughnut', d.Estados, d.ProyectosPorEstado);
    crear('evaluados', 'doughnut', ['Evaluados', 'Pendientes'], [d.Evaluados, d.Pendientes]);
})();
