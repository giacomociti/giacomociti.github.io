var path = require("path");

module.exports = {
    mode: "production",
    entry: "./src/App.fs.js",
    output: {
        path: path.join(__dirname, "../../content/model-based-testing"),
        filename: "main.js",
    }
}
