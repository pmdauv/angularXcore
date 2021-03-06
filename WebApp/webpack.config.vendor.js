var isDevBuild = process.argv.indexOf('--env.prod') < 0;
console.log("webpack.config.vendor isDevBuild", isDevBuild);

var path = require('path');
var webpack = require('webpack');
var ExtractTextPlugin = require('extract-text-webpack-plugin');
var extractCSS = new ExtractTextPlugin('vendor.css');

module.exports = {
    resolve: {
        extensions: ['.ts', '.js']
    },
    module: {
        rules: [
            { test: /\.(png|woff|woff2|eot|ttf|svg)(\?|$)/, loader: 'url-loader?limit=100000' },
            {
                test: /\.css(\?|$)/,
                use: extractCSS.extract({
                    fallback: 'style-loader',
                    use: [
                        { loader: 'css-loader', options: { importLoaders: 1, minimize: true } }
                    ]
                })
            }

        ]
    },
    entry: {
        vendor: [
            '@angular/animations',
            '@angular/animations/browser',
            '@angular/common',
            '@angular/compiler',
            '@angular/core',
            '@angular/http',
            '@angular/platform-browser',
            '@angular/platform-browser-dynamic',
            '@angular/router',
            '@angular/platform-server',
            'angular-2-local-storage',
            'angular-l10n',
            'aspnet-prerendering',
            'bootstrap',
            'bootstrap/dist/css/bootstrap.css',
            'es6-shim',
            'es6-promise',
            'font-awesome/css/font-awesome.css',
            'jquery',
            'reflect-metadata',
            'tether',
            'tether/dist/css/tether.css',
            'zone.js'
        ]
    },
    output: {
        path: path.join(__dirname, 'wwwroot', 'dist'),
        filename: '[name].js',
        library: '[name]_[hash]'
    },
    plugins: [
        extractCSS,
        new webpack.ProvidePlugin({ $: 'jquery', jQuery: 'jquery', 'window.Tether': 'tether', 'Tether': 'tether' }), // Maps these identifiers to the jQuery package (because Bootstrap expects it to be a global variable)
        //new webpack.optimize.OccurrenceOrderPlugin(),
        new webpack.DllPlugin({
            path: path.join(__dirname, 'wwwroot', 'dist', '[name]-manifest.json'),
            name: '[name]_[hash]'
        })
    ].concat(isDevBuild ? [] : [
        new webpack.optimize.UglifyJsPlugin({ compress: { warnings: false } })
    ])
};
