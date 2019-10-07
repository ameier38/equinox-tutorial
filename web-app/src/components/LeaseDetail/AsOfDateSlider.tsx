import React from 'react'
import { Slider, Typography } from '@material-ui/core'

type AsOfDateSliderProps = {
    startDate: Date,
    endDate: Date,
    onAsAtChange: (dt:Date) => void,
    onAsOnChange: (dt:Date) => void,
}

export const AsOfDateSlider: React.FC<AsOfDateSliderProps> = ({
    startDate, 
    endDate,
    onAsAtChange,
    onAsOnChange
}) => {
    const createDateRange = (start:Date, end:Date) => {
        let arr = new Array<Date>()
        for (let dt = start; dt <= end; dt.setDate(dt.getDate() + 1)) {
            arr.push(new Date(dt))
        }
        return arr
    } 
    const marks = createDateRange(startDate, endDate).map((dt, i) => ({value: i, dateValue: dt, label: dt.toISOString()}))
    const onAsAtValueChange = (e:React.ChangeEvent<{}>, value:number | number[]) => {
        let dt = marks.find(mark => mark.value == value)!.dateValue
        onAsAtChange(dt)
    }
    const onAsOnValueChange = (e:React.ChangeEvent<{}>, value:number | number[]) => {
        let dt = marks.find(mark => mark.value == value)!.dateValue
        onAsOnChange(dt)
    }
    return (
        <React.Fragment>
            <Typography id='as-at-slider'>As At</Typography>
            <Slider
                aria-labelledby='as-at-slider'
                defaultValue={marks.length - 1}
                step={null}
                marks
                onChange={onAsAtValueChange} />
            <Typography id='as-on-slider'>As On</Typography>
            <Slider
                aria-labelledby='as-on-slider'
                defaultValue={marks.length - 1}
                step={null}
                marks
                onChange={onAsOnValueChange} />
        </React.Fragment>
    )
}
