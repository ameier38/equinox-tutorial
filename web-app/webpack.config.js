// Note this only includes basic configuration for development mode.
// For a more comprehensive configuration check:
// https://github.com/fable-compiler/webpack-config-template

const DotenvPlugin = require("dotenv-webpack")
const path = require("path")
const webpack = require("webpack");

module.exports = (env, argv) => {
    const mode = argv.mode

    return {
        mode: mode,
        entry: './src/App/App.fsproj',
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
                new DotenvPlugin({
                    path: path.join(__dirname, '.env'),
                    silent: true,
                    systemvars: true
                }),
                new webpack.HotModuleReplacementPlugin(),
            ]
            :
            [
                new DotenvPlugin({
                    path: path.join(__dirname, '.env'),
                    silent: true,
                    systemvars: true
                }),
            ],
        module: {
            rules: [
                { 
                    test: /\.fs(x|proj)?$/,
                    use: {
                        loader: "fable-loader",
                        options: {
                            define: mode === "development" ? ["DEVELOPMENT"] : []
                        }
                    },
                },
                {
                    test: /\.(png|jpe?g|gif|svg)$/i,
                    use: "file-loader"
                }
            ]
        }
    }
} 
