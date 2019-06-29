import React from 'react'
import ReactDOM from 'react-dom'
import ApolloClient from 'apollo-boost'
import { ApolloProvider } from 'react-apollo'
import { ThemeProvider } from '@material-ui/styles'
import { createMuiTheme } from '@material-ui/core/styles'
import CssBaseline from '@material-ui/core/CssBaseline'
import './index.css'
import App from './App'
import * as serviceWorker from './serviceWorker'

const theme = createMuiTheme({
    palette: {
        type: 'dark'
    }
})

const client = new ApolloClient({
    uri: 'http://localhost:4000'
})

const Root = () => (
    <ThemeProvider theme={theme}>
        <ApolloProvider client={client}>
            <CssBaseline />
            <App />
        </ApolloProvider>
    </ThemeProvider>
)

ReactDOM.render(<Root />, document.getElementById('root'))

// If you want your app to work offline and load faster, you can change
// unregister() to register() below. Note this comes with some pitfalls.
// Learn more about service workers: https://bit.ly/CRA-PWA
serviceWorker.unregister()
