import React from 'react'


type TotalScheduledCardProps = {
  setAsOfDate: (asOf:AsOfDate) => void,
  leaseId: string,
  totalScheduled: number
}

const TotalScheduledCard: React.FC<TotalScheduledCardProps> = 
  ({ setAsOfDate, leaseId, totalScheduled }) => {
    const classes = useStyles()
    const [dialogOpen, setDialogOpen] = useState(false)

    return (
      <>
        <Card className={classes.lightPaper}>
          <CardContent>
            <Typography gutterBottom variant='h6'>
              Total Scheduled: {moneyFormatter.format(totalScheduled)}
            </Typography>
          </CardContent>
          <CardActions>
            <Button
              color='primary'
              onClick={() => setDialogOpen(true)}>
              Schedule Payment
            </Button>
          </CardActions>
        </Card>
        <SchedulePaymentDialog
          setAsOfDate={setAsOfDate}
          leaseId={leaseId}
          open={dialogOpen}
          setOpen={setDialogOpen} />
      </>
    )
  }
