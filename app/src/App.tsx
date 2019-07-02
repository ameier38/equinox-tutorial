import React, { useState } from 'react';
import { makeStyles, createStyles, Theme } from '@material-ui/core/styles'
import Container from '@material-ui/core/Container'
import AppBar from '@material-ui/core/AppBar'
import Toolbar from '@material-ui/core/Toolbar'
import Typography from '@material-ui/core/Typography'
import Fab from '@material-ui/core/Fab'
import AddIcon from '@material-ui/icons/Add'
import LeaseTable from './LeaseTable'
import LeaseForm from './LeaseForm'

const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    container: {
      paddingTop: 20,
    },
    fab: {
      position: 'absolute',
      right: 10,
      bottom: 10,
    }
  })
)

const App: React.FC = () => {
  const classes = useStyles()
  const [formOpen, setFormOpen] = useState(false)

  const handleFabClick = () => {
    setFormOpen(!formOpen)
  }

  return (
    <>
      <AppBar position="static" color="primary">
        <Toolbar>
          <Typography variant="h6" color="inherit">
            Equinox Tutorial
          </Typography>
        </Toolbar>
      </AppBar>
      <Container className={classes.container} maxWidth="lg">
        <LeaseTable />
        <LeaseForm
          open={formOpen}
          setOpen={setFormOpen} />
        <Fab 
          color="primary" 
          aria-label="Add" 
          className={classes.fab}
          onClick={handleFabClick}>
          <AddIcon />
        </Fab>
      </Container>
    </>
  )
}

export default App;
