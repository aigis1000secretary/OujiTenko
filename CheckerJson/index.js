const fs = require("fs");
const path = require("path");

// get local file list
let getFileList = function (dirPath) {
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

let rawDataToCsv = function (rawPath, csvPath) {
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

const main = function () {

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

    
    // let resources = "./Resources";
    // if (!fs.existsSync(cardsTxt)) return;

    // let icons = getFileList(cardsTxt);

    // console.table(icons);
    // for(let i in icons){
    //     console.log()
    // }


}; main();

