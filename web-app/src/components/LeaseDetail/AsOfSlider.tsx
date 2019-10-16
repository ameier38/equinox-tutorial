import React, { useState, useEffect } from 'react'
import { createStyles, makeStyles } from '@material-ui/styles'
import { Theme } from '@material-ui/core/styles'
import { Slider, Typography } from '@material-ui/core'
import moment from 'moment'
import { LeaseEvent } from '../../generated/graphql'
import { Event } from './types'

const useStyles = makeStyles((theme: Theme) =>
    createStyles({
        root: {
            padding: theme.spacing(1)
        },
        mark: {
            height: 8,
            marginTop: -3,
            backgroundColor: theme.palette.primary.main
        }
    })
)

type AsOfSliderProps = {
    leaseEvents: LeaseEvent[],
    dispatch: React.Dispatch<Event>
}

type Mark = {
    value: number,
    dateValue: Date
}

export const AsOfSlider: React.FC<AsOfSliderProps> = ({ 
    leaseEvents,
    dispatch
}) => {
    const classes = useStyles()
    const [localAsAt, setLocalAsAt] = useState<Date|null>(null)
    const [asAtValue, setAsAtValue] = useState(0)
    const [localAsOn, setLocalAsOn] = useState<Date|null>(null)
    const [asOnValue, setAsOnValue] = useState(0)
    const [asOnMarks, setAsOnMarks] = useState<Mark[]>([])
    const [asAtMarks, setAsAtMarks] = useState<Mark[]>([])


    const onAsAtValueChange = (e:React.ChangeEvent<{}>, value:number | number[]) => {
        if (typeof value === 'number') {
            const mark = asAtMarks.find(m => m.value === value)
            if (mark) {
                setLocalAsAt(mark.dateValue)
                setAsAtValue(mark.value)
            }
        }
    }

    const onAsAtValueChangeCommitted = (e:React.ChangeEvent<{}>, value:number | number[]) => {
        if (typeof value === 'number') {
            const mark = asAtMarks.find(m => m.value === value)
            if (mark && localAsAt) {
                dispatch({type: 'AS_OF_UPDATED', asOf: { asAt: localAsAt }})
            }
        }
    }

    const onAsOnValueChange = (e:React.ChangeEvent<{}>, value:number | number[]) => {
        if (typeof value === 'number') {
            const mark = asOnMarks.find(m => m.value === value)
            if (mark) {
                setLocalAsOn(mark.dateValue)
                setAsOnValue(mark.value)
            }
        }
    }

    const onAsOnValueChangeCommitted = (e:React.ChangeEvent<{}>, value:number | number[]) => {
        if (typeof value === 'number') {
            const mark = asOnMarks.find(m => m.value === value)
            if (mark && localAsOn) {
                dispatch({type: 'AS_OF_UPDATED', asOf: { asOn: localAsOn }})
            }
        }
    }

    useEffect(() => {
        const now = moment.utc().toDate()

        const convertedLeaseEvents = leaseEvents.map(e => ({
                ...e,
                eventCreatedTime: new Date(e.eventCreatedTime),
                eventEffectiveDate: new Date(e.eventEffectiveDate)
            }))

        const asAts = convertedLeaseEvents.sort((a, b) => {
                if (a.eventCreatedTime > b.eventCreatedTime) return 1
                if (a.eventCreatedTime < b.eventCreatedTime) return -1
                return 0
            }).map((e, idx) => {
                return ({
                    value: idx,
                    dateValue: e.eventCreatedTime,
                })
            })

        asAts.push({value: asAts.length, dateValue: now})

        setAsAtMarks(asAts)

        const asOns = convertedLeaseEvents.sort((a, b) => {
                if (a.eventEffectiveDate > b.eventEffectiveDate) return 1
                if (a.eventEffectiveDate < b.eventEffectiveDate) return -1
                return 0
            }).map((e, idx) => {
                return ({
                    value: idx,
                    dateValue: e.eventEffectiveDate,
                })
            })
        
        asOns.push({value: asOns.length, dateValue: now})

        setAsOnMarks(asOns)

        if (asAts.length > 0) {
            const lastIndex = asAts.length - 1
            setLocalAsAt(asAts[lastIndex].dateValue)
            setAsAtValue(lastIndex)
        }
        if (asOns.length > 0) {
            const lastIndex = asOns.length - 1
            setLocalAsOn(asOns[lastIndex].dateValue)
            setAsOnValue(lastIndex)
        }
    }, [leaseEvents])

    return (
        <div className={classes.root}>
            <Typography id='as-at-slider'>As At: <span>{localAsAt && localAsAt.toISOString()}</span></Typography>
            <Slider
                classes={{mark: classes.mark}}
                aria-labelledby='as-at-slider'
                valueLabelDisplay='off'
                step={null}
                min={0}
                max={asAtMarks.length - 1}
                marks={asAtMarks}
                value={asAtValue}
                onChange={onAsAtValueChange}
                onChangeCommitted={onAsAtValueChangeCommitted} />
            <Typography id='as-on-slider'>As On: <span>{localAsOn && localAsOn.toISOString()}</span></Typography>
            <Slider
                classes={{mark: classes.mark}}
                aria-labelledby='as-on-slider'
                valueLabelDisplay='off'
                step={null}
                marks={asOnMarks}
                min={0}
                max={asOnMarks.length - 1}
                value={asOnValue}
                onChange={onAsOnValueChange}
                onChangeCommitted={onAsOnValueChangeCommitted} />
        </div>
    )
}
