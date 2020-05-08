const styleUnChecked = "opacity:0.4;";
const styleChecked = "opacity:1.0;";
const styleSize = "75";

// url parameter method
function _atob(base32Str) {
    let base2Str = "";
    let l = base32Str.length;
    if (l > 205) {
        base32Str = base32Str.padEnd((parseInt(l / 205) + 1) * 205, "0");
    }
    let i = 0;
    while (l - i > 0) {
        base2Str += parseInt(base32Str.substr(i, 205), 32).toString(2);
        i += 205;
    }
    return base2Str;
}
function _btoa(base2Str) {
    let base32Str = "";
    let l = base2Str.length;
    if (l > 1024) {
        base2Str = base2Str.padEnd((parseInt(l / 1024) + 1) * 1024, "0");
    }
    let i = 0;
    while (l - i > 0) {
        base32Str += parseInt(base2Str.substr(i, 1024), 2).toString(32);
        i += 1024;
    }
    return base32Str;
}

function getUrlFlags() {
    // URL obj
    let url = new URL(document.URL);
    let params = url.searchParams;

    // get url data
    let urlData = params.get("data");

    // return flag list
    return urlData ? _atob(urlData) : "";
}
function setUrlFlags(flagList) {
    // URL obj
    let url = new URL(document.URL);
    let params = url.searchParams;

    // make url data from flag list
    let urlData = _btoa(flagList);

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
    // return flagArray.reverse().join("").replace(/^0*/, "");
    return flagArray.join("").replace(/^0*/, "");
}
function setIconFlags(flagList) {
    // make flag list
    // let flagArray = flagList.split("").reverse();
    let flagArray = flagList.split("");

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
        // icon.title = charaData[i].name;
        icon.title = [
            charaData[i].name,
            "id: " + charaData[i].id,
            "rare: " + charaData[i].rare,
            "classId: " + charaData[i].classId
        ].join("\n");
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

// return By Rare
function returnByRare() {
    let iconbox = document.getElementsByClassName("iconbox")[0];
    bFlag = false;
    for (let i = 0; i < iconbox.childElementCount; ++i) {
        let a = iconbox.children[i];
        let b = iconbox.children[i + 1];
        if (!a || !b || a.tagName != "IMG" || b.tagName != "IMG") continue;

        let aData = charaData.find(obj => obj.id == a.id);
        let bData = charaData.find(obj => obj.id == b.id);

        if ((aData && bData) && aData.rare != bData.rare) {
            let br = document.createElement("br");
            a.parentNode.insertBefore(br, b);
        }
    }

};

// sort method
function sortByDate(ascending) {
    $(".iconbox").empty();

    charaData.sort(function compare(a, b) {
        // sort by id
        if (a.id != b.id) return (!!ascending == (a.id < b.id)) ? -1 : 1;

        return 0;
    })

    init();
};
function sortByRare(ascending) {
    $(".iconbox").empty();

    charaData.sort(function compare(a, b) {
        // sort by rare
        if (a.rare != b.rare) return (!!ascending == (a.rare < b.rare)) ? -1 : 1;

        // sort by group
        if (a.sortGroupID != b.sortGroupID) return (a.sortGroupID < b.sortGroupID) ? -1 : 1;

        // sort by class
        if (a.classId != b.classId) return (a.classId < b.classId) ? -1 : 1;

        // sort by id
        if (a.id != b.id) return (a.id < b.id) ? -1 : 1;

        return 0;
    })

    init();
    returnByRare();
};
function sortByClass(ascending) {
    $(".iconbox").empty();

    charaData.sort(function compare(a, b) {
        // sort by class
        if (a.classId != b.classId) return (!!ascending == (a.classId < b.classId)) ? -1 : 1;

        // sort by rare
        if (a.rare != b.rare) return (a.rare > b.rare) ? -1 : 1;

        // sort by group
        if (a.sortGroupID != b.sortGroupID) return (a.sortGroupID < b.sortGroupID) ? -1 : 1;

        // sort by id
        if (a.id != b.id) return (a.id < b.id) ? -1 : 1;

        return 0;
    })

    init();
};