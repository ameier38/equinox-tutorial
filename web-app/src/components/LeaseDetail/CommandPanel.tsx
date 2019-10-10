import React from 'react'
import { makeStyles, createStyles } from '@material-ui/styles'
import { Grid, Button } from '@material-ui/core'

const useStyles = makeStyles(() =>
    createStyles({
        button: {
            width: '100%'
        }
    })

)

type CommandPanelProps = {
    setSchedulePaymentDialogOpen: (open:boolean) => void,
    setReceivePaymentDialogOpen: (open:boolean) => void,
}

export const CommandPanel: React.FC<CommandPanelProps> = ({ 
    setSchedulePaymentDialogOpen,
    setReceivePaymentDialogOpen
}) => {
    const classes = useStyles()
    return (
        <Grid container>
            <Grid item xs={6}>
                <Button className={classes.button} onClick={() => setSchedulePaymentDialogOpen(true)} variant='contained'>
                    Schedule Payment
                </Button>
            </Grid>
            <Grid item xs={6}>
                <Button className={classes.button} onClick={() => setReceivePaymentDialogOpen(true)} variant='contained'>
                    Receive Payment
                </Button>
            </Grid>
        </Grid>
    )
}
