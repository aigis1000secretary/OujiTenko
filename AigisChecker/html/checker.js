function init() {
    let iconbox = document.getElementsByClassName("iconbox")[0];

    for (let i in data) {
        if (data[i] == null) continue;

        let icon = document.createElement("img");
        icon.className = "icon";
        icon.id = data[i].id;
        icon.title = data[i].name;
        icon.src = data[i].img;
        icon.width = "50";
        icon.height = "50";

        icon.alt = "false";
        icon.style = icon.alt == "true" ? "opacity:1.0;" : "opacity:0.1;";
        icon.addEventListener("click", function (e) {
            this.alt = this.alt == "true" ? "false" : "true";
            this.style = this.alt == "true" ? "opacity:1.0;" : "opacity:0.1;";
        }, false);
        iconbox.appendChild(icon);
    }
}

function sortByRare(ascending) {
    $(".iconbox").empty();

    // sort by class
    data.sort(function compare(a, b) {
        if (a.classId == b.classId) return 0;
        return (a.classId < b.classId) ? -1 : 1;
    })

    // sort by group
    data.sort(function compare(a, b) {
        if (a.sortGroupID == b.sortGroupID) return 0;
        return (a.sortGroupID < b.sortGroupID) ? -1 : 1;
    })

    // sort by rare
    data.sort(function compare(a, b) {
        if (a.rare == b.rare) return 0;
        return (!!ascending == (a.rare < b.rare)) ? -1 : 1;
    })
    init();
};
function sortByDate(ascending) {
    $(".iconbox").empty();

    // sort by id
    data.sort(function compare(a, b) {
        if (a.id == b.id) return 0;
        return (!!ascending == (a.id < b.id)) ? -1 : 1;
    })
    init();
};
function sortByClass(ascending) {
    $(".iconbox").empty();

    // sort by group
    data.sort(function compare(a, b) {
        if (a.sortGroupID == b.sortGroupID) return 0;
        return (a.sortGroupID < b.sortGroupID) ? -1 : 1;
    })

    // sort by rare
    data.sort(function compare(a, b) {
        if (a.rare == b.rare) return 0;
        return (a.rare < b.rare) ? -1 : 1;
    })

    // sort by class
    data.sort(function compare(a, b) {
        if (a.classId == b.classId) return 0;
        return (!!ascending == (a.classId < b.classId)) ? -1 : 1;
    })

    init();
};