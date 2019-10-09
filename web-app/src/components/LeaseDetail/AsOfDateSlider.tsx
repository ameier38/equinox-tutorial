import React from 'react'
import { Slider, Typography } from '@material-ui/core'
import moment from 'moment'

type AsOfDateSliderProps = {
    startDate: Date,
    updatedTime: Date,
    asAt: Date,
    setAsAt: (dt:Date) => void,
    asOn: Date,
    setAsOn: (dt:Date) => void
}

export const AsOfDateSlider: React.FC<AsOfDateSliderProps> = ({ 
    startDate,
    updatedTime,
    asAt,
    setAsAt,
    asOn,
    setAsOn 
}) => {
    const onAsAtValueChange = (e:React.ChangeEvent<{}>, value:number | number[]) => {
        if (typeof value === 'number') {
            const asAt = moment(updatedTime).add(value, 'seconds').toDate()
            setAsAt(asAt)
        }
    }
    const onAsOnValueChange = (e:React.ChangeEvent<{}>, value:number | number[]) => {
        if (typeof value === 'number') {
            const asOn = moment(startDate).add(value, 'days').endOf('day').toDate()
            setAsOn(asOn)
        }
    }
    return (
        <React.Fragment>
            <Typography id='as-at-slider'>As At: <span>{asAt.toISOString()}</span></Typography>
            <Slider
                aria-labelledby='as-at-slider'
                valueLabelDisplay='off'
                defaultValue={0}
                step={1}
                min={-100}
                max={0}
                onChange={onAsAtValueChange} />
            <Typography id='as-on-slider'>As On: <span>{asOn.toISOString()}</span></Typography>
            <Slider
                aria-labelledby='as-on-slider'
                valueLabelDisplay='off'
                defaultValue={0}
                step={1}
                min={0}
                max={100}
                onChange={onAsOnValueChange} />
        </React.Fragment>
    )
}
