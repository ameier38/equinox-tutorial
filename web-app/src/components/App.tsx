import React from 'react';
import { BrowserRouter as Router, Route, Switch, useHistory } from 'react-router-dom'
import { makeStyles, createStyles } from '@material-ui/core/styles'
import { 
    Container,
    AppBar,
    Toolbar,
    Typography,
    Button
} from '@material-ui/core'
import { LeaseSummary } from './LeaseSummary'
import { LeaseDetail } from './LeaseDetail'

const useStyles = makeStyles(() =>
  createStyles({
    container: {
      paddingTop: 20,
    },
  })
)

const TitleButton: React.FC = () => {
    const history = useHistory()
    return (
        <Button onClick={() => history.push('/')}>
            <Typography variant="h6" color="inherit">
                Equinox Tutorial
            </Typography>
        </Button>
    )
}

export const App: React.FC = () => {
    const classes = useStyles()

    return (
        <Router>
            <AppBar position='sticky' color='primary'>
                <Toolbar>
                    <TitleButton />
                </Toolbar>
            </AppBar>
            <Container className={classes.container} maxWidth='lg'>
                <Switch>
                    <Route exact path='/' component={LeaseSummary} />
                    <Route path='/:leaseId' component={LeaseDetail} />
                </Switch>
            </Container>
        </Router>
    )
}
