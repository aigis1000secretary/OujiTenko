const fs = require("fs");
const path = require("path");

// get local file list
let getFileList = function(dirPath) {
    let result = [];
    let apiResult = fs.readdirSync(dirPath);
    for (let i in apiResult) {
        if (fs.lstatSync(dirPath + "/" + apiResult[i]).isDirectory()) {
            result = result.concat(getFileList(dirPath + "/" + apiResult[i]));
        } else if (path.extname(dirPath + "/" + apiResult[i]) == ".png" && apiResult[i] != "altx.png") {
            result.push(dirPath + "/" + apiResult[i]);
        }
    }
    return result;
};

let rawDataToCsv = function(rawPath, csvPath) {
    let raw = fs.readFileSync(rawPath).toString();
    let row0;

    // trim
    raw = raw.replace(/[\s]*\n[\s]*/g, "\n");

    // space in string
    raw = raw.replace(/　/g, " ");
    raw = raw.replace(/\"\"/g, "@");
    let reg = /\"[^\"\n]+\"/;
    while (reg.test(raw)) {
        row0 = reg.exec(raw)[0];
        let newRow = row0;
        newRow = newRow.replace(/\"/g, "").trim();
        newRow = newRow.replace(/ /g, "@");
        raw = raw.replace(row0, newRow);
    }
    reg = /[^A-Za-z0-9\- ] +[^A-Za-z0-9\- ]/;
    while (reg.test(raw)) {
        row0 = reg.exec(raw)[0];
        let newRow = row0;
        newRow = newRow.replace(/ /g, "@");
        raw = raw.replace(row0, newRow);
    }

    // space
    raw = raw.replace(/ +/g, ",");
    raw = raw.replace(/@/g, " ");
    // empty srting ("")
    raw = raw.replace(/, ,/g, ",,");
    // trim 
    raw = raw.trim();

    // write to file
    fs.writeFileSync(csvPath, raw);
    console.log("fs.writeFileSync(", csvPath, ")");

    // return data
    let result = [];
    let rows = raw.split("\n");
    for (let i in rows) {
        result.push(rows[i].split(","));
    }
    return result;
};

let encodeBase64 = function(file) {
    // read binary data
    var bitmap = fs.readFileSync(file);
    // convert binary data to base64 encoded string
    return Buffer.from(bitmap).toString('base64');
}

const main = function() {
    // check resources
    let resources = "./Resources";
    if (!fs.existsSync(resources)) { consoe.log("!fs.existsSync(resources)"); return; }

    // raw data path
    let cardsTxt = "./Resources/cards.txt";
    let classTxt = "./Resources/PlayerUnitTable.aar/002_ClassData.atb/ALTB_cldt.txt";

    // set data list index
    let temp = [];
    temp = rawDataToCsv(cardsTxt, "cards.csv");
    let cardsData = [];
    for (let i in temp) {
        let id = parseInt(temp[i][1]);
        if (!isNaN(id)) {
            cardsData[id] = temp[i];
        }
    }
    temp = rawDataToCsv(classTxt, "class.csv");
    let classData = [];
    for (let i in temp) {
        let id = parseInt(temp[i][0]);
        if (!isNaN(id)) {
            classData[id] = temp[i];
        }
    }

    // console.table(cardsData)
    // console.table(classData)

    // result
    let resultJson = [];

    // get png list
    let icons = getFileList(resources + "/ico_00.aar");

    // console.table(icons);
    let id;
    for (let i in icons) {
        // var
        let iconPath = icons[i];
        id = parseInt(path.basename(iconPath).replace("_001.png", ""));

        // set json data
        if (!cardsData[id]) continue;
        let name = cardsData[id][0];
        let rare = parseInt(cardsData[id][7]);
        let classId = parseInt(cardsData[id][2]);
        let sortGroupID = classData[classId] ? parseInt(classData[classId][39]) : 0;
        let placeType = classData[classId] ? parseInt(classData[classId][41]) : 0;
        let kind = parseInt(cardsData[id][6]);
        let isEvent = (parseInt(cardsData[id][48]) <= 15) ? 1 : 0; // _TradePoint
        let assign = parseInt(cardsData[id][60]);
        let genus = parseInt(cardsData[id][61]);
        // let identity = parseInt(cardsData[id][62]);

        switch (rare) {
            case 11:
                rare = 5.1;
                break;
            case 10:
                rare = 4.1;
                break;
            case 7:
                rare = 3.5;
                break;
        }

        // skip who not a unit
        let skipList = [1];
        if (skipList.indexOf(id) != -1) continue;
        // skip token
        let sellPrice = parseInt(cardsData[id][11]);
        if (sellPrice == 0) continue;
        // skip seirei
        if (sortGroupID == 10) continue;

        let obj = {
            id,
            name,
            rare,
            classId,
            sortGroupID,
            placeType,
            kind,
            isEvent,
            assign,
            genus,
            // identity,
            img: "data:image/png;base64," + encodeBase64(iconPath),
        };
        // resultJson.push(obj);
        resultJson[id] = obj;
    }

    // write to file
    let cardsJs = ["var maxCid = " + (id + 1) + ";\nvar charaData = ["];
    for (let i in resultJson) {
        cardsJs.push("\t" + JSON.stringify(resultJson[i], null, 1).replace(/\s*\n\s*/g, "\t") + ",");
    }
    cardsJs.push("]");

    fs.writeFileSync("./html/cards.js", cardsJs.join("\n"));
    console.log("fs.writeFileSync( cards.js )");

};
main();