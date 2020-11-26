const DotenvPlugin = require("dotenv-webpack")
const HtmlWebpackPlugin = require("html-webpack-plugin")
const path = require("path")
const webpack = require("webpack");

module.exports = (env, argv) => {
    const mode = argv.mode
    const entry = argv.entry

    const htmlPlugin = new HtmlWebpackPlugin({
        filename: 'index.html',
        template: path.resolve('./src/App/index.html'),
        favicon: path.resolve('./src/App/images/rocket.svg')
    })

    const dotenvPlugin = new DotenvPlugin({
        path: path.join(__dirname, '.env'),
        silent: true,
        systemvars: true
    })

    return {
        mode: mode,
        entry: entry,
        output: {
            path: path.join(__dirname, "dist"),
            filename: "main.js",
        },
        devServer: {
            contentBase: path.join(__dirname, "dist"),
            port: 3000,
            hot: true,
            inline: true,
            // NB: required so that webpack will go to index.html on not found
            historyApiFallback: true
        },
        // NB: so webpack works with docker
        watchOptions: {
            poll: true
        },
        plugins: mode === 'development' ?
            [
                dotenvPlugin,
                htmlPlugin,
                new webpack.HotModuleReplacementPlugin(),
            ]
            :
            [
                dotenvPlugin,
                htmlPlugin,
            ],
        module: {
            rules: [
                { 
                    test: /\.fs(x|proj)?$/,
                    use: "fable-loader"
                },
                {
                    test: /\.(png|jpe?g|gif|svg)$/i,
                    use: "file-loader"
                }
            ]
        }
    }
} 
