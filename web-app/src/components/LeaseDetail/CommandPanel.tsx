import React from 'react'
import { makeStyles, createStyles } from '@material-ui/styles'
import { Theme } from '@material-ui/core/styles'
import { Grid, Button } from '@material-ui/core'
import { Event } from './types'

const useStyles = makeStyles((theme:Theme) =>
    createStyles({
        root: {
            padding: theme.spacing(1)
        },
        button: {
            width: '100%'
        }
    })
)

type CommandPanelProps = {
    dispatch: React.Dispatch<Event>
}

export const CommandPanel: React.FC<CommandPanelProps> = ({ 
    dispatch
}) => {
    const classes = useStyles()
    const openSchedulePaymentDialog = () => {
        dispatch({type: 'SCHEDULE_PAYMENT_DIALOG_TOGGLED', open: true})
    }
    const openReceivePaymentDialog = () => {
        dispatch({type: 'RECEIVE_PAYMENT_DIALOG_TOGGLED', open: true})
    }
    return (
        <div className={classes.root}>
            <Grid container spacing={2}>
                <Grid item xs={6}>
                    <Button 
                        className={classes.button} 
                        color='primary'
                        onClick={openSchedulePaymentDialog} 
                        variant='contained'>
                        Schedule Payment
                    </Button>
                </Grid>
                <Grid item xs={6}>
                    <Button 
                        className={classes.button} 
                        color='primary'
                        onClick={openReceivePaymentDialog} 
                        variant='contained'>
                        Receive Payment
                    </Button>
                </Grid>
            </Grid>
        </div>
    )
}
