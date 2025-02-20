const pages = [
    { name: 'Home', path: '/index.html' },
    { name: 'The lost art of data abstraction', path: '/2018/05/26/The-lost-art-of-data-abstraction.html' },
    { name: 'Xsd type provider and nillable elements', path: '/2018/07/22/Xsd-type-provider-and-nillable-elements.html' },
    { name: 'Abstract types are more equal', path: '/2019/01/02/Abstract-types-are-more-equal.html' },
    { name: 'Maybe RDF', path: '/2021/01/29/Maybe-RDF.html' },
    { name: 'Model based testing made simplistic', path: '/2021/08/19/Model-based-testing-made-simplistic.html' },
]

document.addEventListener('DOMContentLoaded', () => {
    const nav = document.getElementById('nav')
    const ul = document.createElement('ul')
    nav.appendChild(ul)
    pages.forEach(page => {
        const li = document.createElement('li')
        const a = document.createElement('a')
        a.innerText = page.name
        a.href = page.path
        ul.appendChild(li)
        li.appendChild(a)
    })
})