function init() {
    let iconbox = document.getElementsByClassName("iconbox")[0];

    for (let i in data) {
        if (data[i] == null) continue;

        let icon = document.createElement("img");
        icon.className = "icon";
        icon.id = data[i].id;
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

function doSort(ascending) {
    $(".iconbox").empty();
    data.sort(function compare(a, b) {
        if (a.id == b.id) return 0;
        return (!!ascending == !!(a.id < b.id)) ? -1 : 1;
    })
    init();
};