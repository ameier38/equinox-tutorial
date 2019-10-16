import React, { useRef, useState, useEffect, useCallback } from 'react'
import { createStyles, makeStyles } from '@material-ui/styles'
import { purple } from '@material-ui/core/colors'
import { LinearProgress } from '@material-ui/core'
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

const margin = 20
const svgHeight = 300
const plotHeight = svgHeight - (2 * margin)

export const BalancePlot: React.FC<BalancePlotProps> = ({ getLeaseResult }) => {
    const classes = useStyles()
    const [svgWidth, setSvgWidth] = useState(0)
    const rootRef = useRef<HTMLDivElement>() // no initial value = mutable
    const svgRef = useRef<SVGSVGElement>()
    const {data, loading, error} = getLeaseResult

    const handleResize = () => {
        if (rootRef.current) {
            setSvgWidth(rootRef.current.getBoundingClientRect().width)
        }
    }

    // ref: https://reactjs.org/docs/hooks-faq.html#how-can-i-measure-a-dom-node
    const rootRefCallback = useCallback((node:HTMLDivElement|null) => {
        if (node !== null) {
            rootRef.current = node
            setSvgWidth(node.getBoundingClientRect().width)
        }
    }, [])

    const svgRefCallback = useCallback((node:SVGSVGElement|null) => {
        if (node !== null) {
            svgRef.current = node
            const svg = d3.select(node)
            svg.append('g').attr('id', 'plot').attr('transform', `translate(${margin}, ${margin})`)
            svg.append('g').attr('id', 'x-axis').attr('transform', `translate(${margin}, ${margin + plotHeight})`)
        }
    }, [])

    useEffect(() => {
        window.addEventListener('resize', handleResize)
        return () => window.removeEventListener('resize', handleResize)
    }, [])

    useEffect(() => {
        if (data && svgRef.current) {
            const plotData: Bar[] = [
                {label: 'Total Scheduled', value: data.getLease.totalScheduled},
                {label: 'Total Paid', value: data.getLease.totalPaid},
                {label: 'Amount Due', value: data.getLease.amountDue}
            ]
            const plotWidth = svgWidth! - (2 * margin)

            const svg = d3.select(svgRef.current)
            svg.attr('width', svgWidth!).attr('height', svgHeight)
            const plot = svg.select('#plot')

            const x = d3
                .scaleBand()
                .domain(plotData.map(e => e.label))
                .range([0, plotWidth])
                .padding(0.2)

            const y = d3
                .scaleLinear()
                .domain([0, Math.max(...plotData.map(e => e.value), 1)])
                .range([plotHeight, 0])

            const groupSelection = plot.selectAll<SVGGElement,Bar>('.group').data(plotData)
            groupSelection.exit().remove()
            const mergedGroupSelection = groupSelection
                .enter()
                    .append('g')
                    .attr('class', 'group')
                .merge(groupSelection)

            const rectSelection = mergedGroupSelection.selectAll<SVGRectElement,Bar>('.value').data(d => [d])
            rectSelection
                .enter()
                    .append('rect')
                    .attr('class', 'value')
                    .attr('fill', purple[200])
                .merge(rectSelection)
                    .attr('x', d => x(d.label) || null)
                    .attr('width', x.bandwidth())
                    .attr('y', (d:Bar) => y(d.value))
                    .attr('height', (d:Bar) => plotHeight - y(d.value))

            const textSelection = mergedGroupSelection.selectAll<SVGTextElement,Bar>('.label').data(d => [d])
            textSelection
                .enter()
                    .append('text')
                    .attr('class', 'label')
                .merge(textSelection)
                    .attr('x', d => (x(d.label) || 0) + x.bandwidth() / 2)
                    .attr('y', d => y(d.value) + 30)
                    .attr('text-anchor', 'middle')
                    .text(d => dollarFormatter.format(d.value))

            d3.select(svgRef.current).select<SVGGElement>('#x-axis').call(d3.axisBottom(x))
        }
    }, [data, svgWidth])

    if (error) return <p>{JSON.stringify(error)}</p>

    return (
        <div ref={rootRefCallback} className={classes.root}>
            {loading && <LinearProgress />}
            <svg ref={svgRefCallback} />
        </div>
    )
}
