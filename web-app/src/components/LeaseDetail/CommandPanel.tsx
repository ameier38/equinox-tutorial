import React from 'react'
import { Grid, Button } from '@material-ui/core'

type CommandPanelProps = {
    setSchedulePaymentDialogOpen: (open:boolean) => void,
    setReceivePaymentDialogOpen: (open:boolean) => void,
}

export const CommandPanel: React.FC<CommandPanelProps> = ({ 
    setSchedulePaymentDialogOpen,
    setReceivePaymentDialogOpen
}) => {
    return (
        <Grid container>
            <Grid item xs={6}>
                <Button onClick={() => setSchedulePaymentDialogOpen(true)} variant='contained'>
                    Schedule Payment
                </Button>
            </Grid>
            <Grid item xs={6}>
                <Button onClick={() => setReceivePaymentDialogOpen(true)} variant='contained'>
                    Receive Payment
                </Button>
            </Grid>
        </Grid>
    )
}
