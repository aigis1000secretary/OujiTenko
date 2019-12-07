
const fs = require('fs');
const path = require('path');
const request = require('request');
const iconv = require("iconv-lite");
const cheerio = require("cheerio");

let folderPath = ".\\Resources\\";

let requestGetSync = function (url) {
    return new Promise(function (resolve, reject) {
        request.get(url, { encoding: "binary" }, function (error, response, body) {
            if (error || !body) {
                // console.log(url + "\n" + error);
                reject(error);
                // resolve(null);
            }
            resolve(body);
        });
    });
}

String.prototype.tableToArray = function () {
    let result = [];
    let html = this;
    let i;

    // regexp
    const tbody = /<tbody>[\s\S]*?<\/tbody>/;
    const tr = /<tr[\s\S]*?<\/tr>/;
    const td = /<t[dh][\s\S]*?<\/t[dh]>/;

    if (tbody.test(html)) {
        // get tbody
        html = tbody.exec(html)[0];
        // init table array data
        i = -1;
        while ((i = html.indexOf("<tr>", i + 1)) != -1) {
            result.push([]);
        }

        // get all Column body
        let col = 0;
        while (tr.test(html)) {
            // get single Column body
            let columnBody = tr.exec(html)[0];
            html = html.replace(tr, "");

            // get all Row body
            let row = 0;
            while (td.test(columnBody)) {
                while (result[col][row] == "@") { row++; }
                // split single Cell body
                let cellBody = td.exec(columnBody)[0];
                let cellStyle = /<t[dh][\s\S]*?>/.exec(cellBody)[0]
                // let cellText = cellBody.replace(/<t[dh][\s\S]*?>/, "").replace(/<\/t[dh]>/, "");
                // let cellText = cellBody.replace(/<[\s\S]*?>/g, "");
                let cellText = cellBody.replace(/<[\/]?[ta][\s\S]*?>/g, "").replace(/<[\s\S]*?\"|\"[\s\S]*?>/g, "");
                columnBody = columnBody.replace(td, "");

                // set cell text
                result[col][row] = cellText.trim();
                // set span cell text/flag
                let style = cellStyle.split(" ");
                for (let l in style) {
                    if (style[l].indexOf("rowspan") != -1) {
                        let rowspan = parseInt(/\d+/.exec(style[l]));
                        for (let span = 1; span < rowspan; span++) {
                            result[col + span][row] = "@";
                        }
                    }
                    if (style[l].indexOf("colspan") != -1) {
                        let colspan = parseInt(/\d+/.exec(style[l]));
                        for (let span = 1; span < colspan; span++) {
                            row++;
                            result[col][row] = cellText.trim();
                        }
                    }
                }

                row++;
            }

            // set span cell text
            for (row in result[col]) {
                if (result[col][row] == "@") {
                    result[col][row] = result[col - 1][row];
                }
            }

            col++;
        }
    }
    return result;
}




// init function
let iconCrawler = async function (charaData) {
    let pageUrl = "https://seesaawiki.jp/aigis/d/" + charaData.urlName;
    let body = await requestGetSync(pageUrl).catch(function (error) { console.log(pageUrl + "\n" + error); });
    if (body == null) return;

    let html = iconv.decode(Buffer.from(body, "binary"), "EUC-JP"); // EUC-JP to utf8 // Shift_JIS EUC-JP
    let $ = cheerio.load(html, { decodeEntities: false }); // 載入 body
    $("table").each(function (i, iElem) {
        let tableHtml = $(this).html().replace(/<br>/g, "");
        if (tableHtml.indexOf("アイコン") != -1 && tableHtml.indexOf("ドット絵") != -1 && tableHtml.indexOf("<img" != -1)) // found some img in table
        {
            let table = tableHtml.tableToArray();
            // console.table(table);
            for (let i in table) {
                if (table[i][0] != "アイコン") { continue; }    // get icon row
                let row = table[i];

                // 
                let iconList = [];
                for (let j = 1; j < row.length; ++j) {
                    if (!table[0][j]) { continue; } // check classname

                    let cell = row[j];  // get icon img url
                    if (!/https?:\/\/[a-z0-9./_]+/i.exec(cell)) { continue; }
                    let ext = path.parse(cell).ext;
                    let filename = folderPath + charaData.name + "_" + table[0][j] + ext;

                    // no icon data
                    if (cell == "https://image01.seesaawiki.jp/a/s/aigis/c0c0aaf0b2648db5.png" ||
                        cell == "https://image01.seesaawiki.jp/a/s/aigis/Eyjy4EA5iF.png") continue;

                    let flag = false;
                    for (let i in iconList) {
                        if (iconList[i][0] == cell) { flag = true; }
                    } if (flag) continue; // same icon for two class

                    iconList.push([cell, filename]);

                }
                for (let i in iconList) {
                    let iconRrl = iconList[i][0];
                    let filename = iconList[i][1];
                    requestGetSync(iconRrl)
                        .then(function (imgbody) {
                            console.log(filename + "\n" + iconRrl);
                            fs.writeFileSync(filename, imgbody, { encoding: "Binary" });
                        })
                        .catch(function (error) {
                            console.log(pageUrl + "\n" + error);
                        });
                }
            }
        }
    })
}

const main = async function () {
    // read character list
    let json = fs.readFileSync("CharaDatabase.json");
    let database = [];
    try {
        database = JSON.parse(json);
    } catch (e) {
        database = eval("(" + json + ")");
    }

    // check folder
    if (!fs.existsSync(folderPath)) { fs.mkdirSync(folderPath, { recursive: true }); }

    // iconCrawler(database[150]);
    // iconCrawler({ name: "山賊バーガン", urlName: "%bb%b3%c2%b1%a5%d0%a1%bc%a5%ac%a5%f3" });
    // 50 thread
    let dataList = Object.assign([], database);
    while (dataList.length > 0) {
        let pArray = [];
        // 50 thread
        for (let i = 0, pop; i < 50 && (pop = dataList.pop()); ++i) {
            pArray.push(iconCrawler(pop));
        }
        await Promise.all(pArray);
    }

    console.log("done!");
}; main();