const styleUnChecked = "opacity:0.4;";
const styleChecked = "opacity:1.0;";
const styleSize = "75";

// url parameter method
function getUrlFlags() {
    // URL obj
    let url = new URL(document.URL);
    let params = url.searchParams;

    // get url data
    let urlData = params.get("data");

    // return flag list
    return urlData ? window.atob(urlData) : "";
}
function setUrlFlags(flagList) {
    // URL obj
    let url = new URL(document.URL);
    let params = url.searchParams;

    // make url data from flag list
    let urlData = window.btoa(flagList);

    // set data to url
    params.set("data", urlData);
    history.pushState(null, null, url);
}
function getIconFlags() {
    // read flag from iconbox
    let flagArray = [];
    let maxIndex = 0;
    let iconList = document.getElementsByClassName("iconbox")[0].getElementsByClassName("icon");
    for (let i in iconList) {
        let icon = iconList[i];
        let id = icon.id;
        let flag = (icon.alt == "true") ? "1" : "0";
        flagArray[id] = flag;
        if (!isNaN(id)) {
            maxIndex = Math.max(id, maxIndex);
        }
    }
    for (let i = maxIndex; i >= 0; --i) {
        if (!flagArray[i]) flagArray[i] = "0";
    }

    // make flag list
    return flagArray.reverse().join("").replace(/^0*/, "");
}
function setIconFlags(flagList) {
    // make flag list
    let flagArray = flagList.split("").reverse();

    // set flag to iconbox
    let iconbox = document.getElementsByClassName("iconbox")[0];
    for (let i in iconbox.children) {
        let icon = iconbox.children[i];
        let id = icon.id;
        let flag = (flagArray[id] && flagArray[id] == "1") ? "true" : "false";

        icon.alt = flag;
        icon.style = icon.alt == "true" ? styleChecked : styleUnChecked;
    }
}

// init
function init() {
    let iconbox = document.getElementsByClassName("iconbox")[0];

    for (let i in charaData) {
        if (charaData[i] == null) continue;

        let icon = document.createElement("img");
        icon.className = "icon";
        icon.id = charaData[i].id;
        icon.title = charaData[i].name + "\n" + icon.id;
        icon.src = charaData[i].img;
        icon.width = styleSize;
        icon.height = styleSize;

        icon.alt = "false";
        icon.style = icon.alt == "true" ? styleChecked : styleUnChecked;
        icon.addEventListener("click", function (e) {
            this.alt = this.alt == "true" ? "false" : "true";
            this.style = this.alt == "true" ? styleChecked : styleUnChecked;
            // set url data
            flagList = getIconFlags()
            setUrlFlags(flagList)
        }, false);
        iconbox.appendChild(icon);
    }
    // read url data
    flagList = getUrlFlags()
    setIconFlags(flagList)
}

// sort method
function sortByRare(ascending) {
    $(".iconbox").empty();

    // sort by class
    charaData.sort(function compare(a, b) {
        if (a.classId == b.classId) return 0;
        return (a.classId < b.classId) ? -1 : 1;
    })

    // sort by group
    charaData.sort(function compare(a, b) {
        if (a.sortGroupID == b.sortGroupID) return 0;
        return (a.sortGroupID < b.sortGroupID) ? -1 : 1;
    })

    // sort by rare
    charaData.sort(function compare(a, b) {
        if (a.rare == b.rare) return 0;
        return (!!ascending == (a.rare < b.rare)) ? -1 : 1;
    })
    init();
};
function sortByDate(ascending) {
    $(".iconbox").empty();

    // sort by id
    charaData.sort(function compare(a, b) {
        if (a.id == b.id) return 0;
        return (!!ascending == (a.id < b.id)) ? -1 : 1;
    })
    init();
};
function sortByClass(ascending) {
    $(".iconbox").empty();

    // sort by group
    charaData.sort(function compare(a, b) {
        if (a.sortGroupID == b.sortGroupID) return 0;
        return (a.sortGroupID < b.sortGroupID) ? -1 : 1;
    })

    // sort by rare
    charaData.sort(function compare(a, b) {
        if (a.rare == b.rare) return 0;
        return (a.rare < b.rare) ? -1 : 1;
    })

    // sort by class
    charaData.sort(function compare(a, b) {
        if (a.classId == b.classId) return 0;
        return (!!ascending == (a.classId < b.classId)) ? -1 : 1;
    })

    init();
};