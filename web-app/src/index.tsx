import React from 'react'
import ReactDOM from 'react-dom'
import { GraphQLClient, ClientContext } from 'graphql-hooks'
import { ThemeProvider } from '@material-ui/styles'
import { createMuiTheme } from '@material-ui/core/styles'
import CssBaseline from '@material-ui/core/CssBaseline'
import purple from '@material-ui/core/colors/purple'
import './index.css'
import * as config from './config'
import * as serviceWorker from './serviceWorker'
import { App } from './components/App'

const theme = createMuiTheme({
    palette: {
        type: 'dark',
        primary: purple
    }
})

const client = new GraphQLClient({
    url: config.GraphQLConfig.url
})

const Root = () => (
    <ThemeProvider theme={theme}>
        <ClientContext.Provider value={client}>
            <CssBaseline />
            <App />
        </ClientContext.Provider>
    </ThemeProvider>
)

ReactDOM.render(<Root />, document.getElementById('root'))

// If you want your app to work offline and load faster, you can change
// unregister() to register() below. Note this comes with some pitfalls.
// Learn more about service workers: https://bit.ly/CRA-PWA
serviceWorker.unregister()
