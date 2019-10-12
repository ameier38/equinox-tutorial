import React, { useRef, useEffect } from 'react'
import { createStyles, makeStyles } from '@material-ui/styles'
import { purple } from '@material-ui/core/colors'
import * as d3 from 'd3'
import { UseClientRequestResult } from 'graphql-hooks'
import { Query } from '../../generated/graphql'

const useStyles = makeStyles(() =>
    createStyles({
        root: {
            height: '100%',
            width: '100%',
        }
    })
)

type BalancePlotProps = {
    leaseId: string,
    getLeaseResult: UseClientRequestResult<Query>
}

type Bar = {
    label: string,
    value: number
}

const dollarFormatter = new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency: 'USD'
})

export const BalancePlot: React.FC<BalancePlotProps> = ({
    leaseId,
    getLeaseResult
}) => {
    const classes = useStyles()
    const rootRef = useRef<HTMLDivElement>(null)
    const svgRef = useRef<SVGSVGElement>(null)
    const {data, loading, error} = getLeaseResult

    useEffect(() => {
        if (!loading && data && rootRef.current && svgRef.current) {
            console.log('data', data)
            const { width: domWidth } = rootRef.current.getBoundingClientRect()
            console.log('domWidth', domWidth)
            const plotData: Bar[] = [
                {label: 'Total Scheduled', value: data.getLease.totalScheduled},
                {label: 'Total Paid', value: data.getLease.totalPaid},
                {label: 'Amount Due', value: data.getLease.amountDue}
            ]
            const margin = 20 
            const width = domWidth - 2 * margin
            const height = 300 - 2 * margin

            const x = d3
                .scaleBand()
                .domain(plotData.map(e => e.label))
                .range([0, width])
                .padding(0.2)

            console.log('max y', Math.max(...plotData.map(e => e.value)))

            const y = d3
                .scaleLinear()
                .domain([0, Math.max(...plotData.map(e => e.value))])
                .range([height, 0])

            console.log('y(0)', y(0))
            console.log('height', height)

            const svg = d3.select(svgRef.current)
            svg.attr('width', width + margin).attr('height', height + margin)
            svg.append('g').attr('transform', `translate(${margin}, ${margin})`)

            const groupSelection = svg.selectAll('.group').data(plotData)
            const rectSelection = svg.selectAll<SVGRectElement,Bar>('.value').data(plotData)
            const textSelection = svg.selectAll<SVGTextElement,Bar>('.label').data(plotData)

            groupSelection.enter().append('g')

            rectSelection
                .enter()
                    .append('rect')
                    .attr('class', 'value')
                    .attr('fill', purple[200])
                .merge(rectSelection)
                    .attr('x', d => x(d.label) || null)
                    .attr('width', x.bandwidth())
                    .attr('y', (d:Bar) => y(d.value))
                    .attr('height', (d:Bar) => height - y(d.value))
            textSelection
                .enter()
                    .append('text')
                    .attr('class', 'label')
                .merge(textSelection)
                    .attr('x', d => (x(d.label) || 0) + x.bandwidth() / 2)
                    .attr('y', d => y(d.value) + 30)
                    .attr('text-anchor', 'middle')
                    .text(d => dollarFormatter.format(d.value))

            groupSelection.exit().remove()

            svg.append('g').attr('transform', `translate(0, ${height})`).call(d3.axisBottom(x))
        }
    }, [data])
    
    return (
        <div ref={rootRef} className={classes.root}>
            <svg ref={svgRef}/>
        </div>
    )
}
