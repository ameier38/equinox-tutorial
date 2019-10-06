type TotalPaidCardProps = {
  setAsOfDate: (asOf:AsOfDate) => void,
  leaseId: string,
  totalPaid: number
}

const TotalPaidCard: React.FC<TotalPaidCardProps> = 
  ({ setAsOfDate, leaseId, totalPaid }) => {
    const classes = useStyles()
    const [dialogOpen, setDialogOpen] = useState(false)

    return (
      <>
        <Card className={classes.lightPaper}>
          <CardContent>
            <Typography gutterBottom variant='h6'>
              Total Paid: {moneyFormatter.format(totalPaid)}
            </Typography>
          </CardContent>
          <CardActions>
            <Button
              color='primary'
              onClick={() => setDialogOpen(true)}>
              Receive Payment
            </Button>
          </CardActions>
        </Card>
        <ReceivePaymentDialog
          setAsOfDate={setAsOfDate}
          leaseId={leaseId}
          open={dialogOpen}
          setOpen={setDialogOpen} />
      </>
    )
  }
