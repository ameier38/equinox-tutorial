import React from 'react'
import gql from 'graphql-tag'
import { useQuery, useApolloClient } from '@apollo/react-hooks'
import { Slider, Typography } from '@material-ui/core'
import * as moment from 'moment'

const GET_AS_OF_DATE = gql`
query GetAsOfDate {
    asAt @client
    asOn @client
}`

type GetAsOfDateResponse = {
    asAt: Date
    asOn: Date
}

type AsOfDateSliderProps = {
    startDate: Date
}

export const AsOfDateSlider: React.FC<AsOfDateSliderProps> = ({ startDate }) => {
    const createDateRange = (endDate:Date) => {
        let arr = new Array<Date>()
        for (let dt = startDate; dt <= endDate; dt.setDate(dt.getDate() + 1)) {
            arr.push(new Date(dt))
        }
        return arr
    } 
    const { data } = useQuery<GetAsOfDateResponse>(GET_AS_OF_DATE)
    const client = useApolloClient()
    const endDate = moment.utc().endOf('day').toDate()
    const marks = createDateRange(endDate).map((dt, i) => ({
        value: i, 
        dateValue: dt, 
        label: dt.toISOString()
    }))
    const getMarkValue = (dt:Date) => {
        const mark = marks.find(mark => mark.dateValue === dt)
        if (mark) {
            return mark.value
        }
    }
    const onAsAtValueChange = (e:React.ChangeEvent<{}>, value:number | number[]) => {
        let dt = marks.find(mark => mark.value == value)!.dateValue
        client.writeData({ data: { asAt: dt } })
    }
    const onAsOnValueChange = (e:React.ChangeEvent<{}>, value:number | number[]) => {
        let dt = marks.find(mark => mark.value == value)!.dateValue
        client.writeData({ data: { asOn: dt } })
    }
    return (
        <React.Fragment>
            <Typography id='as-at-slider'>As At</Typography>
            <Slider
                aria-labelledby='as-at-slider'
                defaultValue={data && getMarkValue(data.asAt)}
                step={null}
                marks
                onChange={onAsAtValueChange} />
            <Typography id='as-on-slider'>As On</Typography>
            <Slider
                aria-labelledby='as-on-slider'
                defaultValue={data && getMarkValue(data.asOn)}
                step={null}
                marks
                onChange={onAsOnValueChange} />
        </React.Fragment>
    )
}
