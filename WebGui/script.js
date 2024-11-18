const width = 800;
const height = 600;

const svg = d3.select("#network");

// 创建 <defs> 和 <clipPath>
const defs = svg.append("defs");

const IMAGE_WIDTH = 60;
const IMAGE_HEIGHT = 60;
const NODE_RADIUS = (IMAGE_WIDTH / 2) * 0.67;

defs
  .append("clipPath")
  .attr("id", "circleClip")
  .append("circle")
  .attr("r", NODE_RADIUS) // 设置圆的半径
  .attr("cx", IMAGE_WIDTH / 2) // 设置圆心的 x 坐标
  .attr("cy", IMAGE_HEIGHT / 2); // 设置圆心的 y 坐标

const simulation = d3
  .forceSimulation()
  .force(
    "link",
    d3
      .forceLink()
      .id((d) => d.id)
      .distance(100)
  )
  .force("charge", d3.forceManyBody().strength(-300))
  .force("center", d3.forceCenter(width / 2, height / 2));

const graph = {
  nodes: [
    {
      id: "A",
      img: "https://static.zerotoheroes.com/hearthstone/cardart/256x/BG26_963.jpg",
    },
    {
      id: "B",
      img: "https://static.zerotoheroes.com/hearthstone/cardart/256x/BG26_531.jpg",
    },
    {
      id: "C",
      img: "https://static.zerotoheroes.com/hearthstone/cardart/256x/BG26_535.jpg",
    },
    {
      id: "D",
      img: "https://static.zerotoheroes.com/hearthstone/cardart/256x/BG26_360.jpg",
    },
  ],
  links: [
    { source: "A", target: "B" },
    { source: "A", target: "C" },
    { source: "B", target: "D" },
    { source: "C", target: "D" },
  ],
};

const link = svg
  .append("g")
  .attr("class", "links")
  .selectAll("line")
  .data(graph.links)
  .enter()
  .append("line")
  .attr("class", "link")
  .style("stroke", (d) => {
    return d.weight ? "blue" : "gray";
  })
  .style("stroke-width", (d) => {
    return d.weight ? 2 : 1;
  })
  .style("stroke-dasharray", (d) => {
    return d.type === "dashed" ? "5,5" : "none";
  });

const node = svg
  .append("g")
  .attr("class", "nodes")
  .selectAll("g")
  .data(graph.nodes)
  .enter()
  .append("g")
  .attr("class", "node")
  .call(
    d3.drag().on("start", dragstarted).on("drag", dragged).on("end", dragended)
  );

node
  .append("image")
  .style("opacity", 0.8)
  .attr("xlink:href", (d) => d.img)
  .attr("width", IMAGE_WIDTH)
  .attr("height", IMAGE_HEIGHT)
  .attr("clip-path", "url(#circleClip)");

node.append("title").text((d) => d.id);

node
  .on("mouseover", function (event, d) {
    // set image in current node opacity to 1
    d3.select(this).select("image").style("opacity", 1);

    // 高亮当前节点
    const currentNode = d3
      .select(this)
      .append("circle")
      .attr("class", "hover-circle")
      .attr("r", NODE_RADIUS + 5)
      .attr("fill", "none")
      .attr("stroke", "orange")
      .attr("stroke-width", 2)
      .attr("transform", `translate(${IMAGE_WIDTH / 2},${IMAGE_HEIGHT / 2})`)
      .style("opacity", 0) // 初始透明度为 0
      .transition() // 开始过渡
      .duration(300) // 过渡持续时间
      .style("opacity", 1); // 逐渐变为不透明

    // 高亮相邻节点并改变连线样式
    graph.links.forEach((link) => {
      if (link.source.id === d.id || link.target.id === d.id) {
        // 改变连线的颜色和宽度
        const filteredLines = d3
          .selectAll("line") // 假设您使用 <line> 元素来表示连线
          .filter(
            (l) =>
              (l.source.id === link.source.id &&
                l.target.id === link.target.id) ||
              (l.source.id === link.target.id && l.target.id === link.source.id)
          );

        // 输出过滤后的连线到控制台
        console.log(filteredLines.nodes()); // 输出过滤后的连线节点

        // 改变连线的颜色和宽度
        filteredLines
          .transition()
          .duration(300)
          .style("stroke", "green") // 改变连线颜色为绿色
          .style("stroke-width", 4); // 改变连线宽度

        // 为相邻节点添加光圈
        d3.select(
          node
            .nodes()
            .find(
              (n) =>
                n.__data__.id ===
                (link.source.id === d.id ? link.target.id : link.source.id)
            )
        )
          .append("circle")
          .attr("class", "hover-circle")
          .attr("r", NODE_RADIUS + 5)
          .attr("fill", "none")
          .attr("stroke", "orange")
          .attr("stroke-width", 2)
          .attr("transform", "translate(30, 30)")
          .style("opacity", 0) // 初始透明度为 0
          .transition() // 开始过渡
          .duration(300) // 过渡持续时间
          .style("opacity", 1); // 逐渐变为不透明
      }
    });
  })
  .on("mouseout", function (event, d) {
    // 移除当前节点的高亮圆圈
    d3.select(this)
      .selectAll(".hover-circle")
      .transition() // 开始过渡
      .duration(300) // 过渡持续时间
      .style("opacity", 0) // 逐渐变为透明
      .remove(); // 过渡结束后移除

    // 移除相邻节点的高亮圆圈
    graph.links.forEach((link) => {
      if (link.source.id === d.id || link.target.id === d.id) {
        d3.select(
          node
            .nodes()
            .find(
              (n) =>
                n.__data__.id ===
                (link.source.id === d.id ? link.target.id : link.source.id)
            )
        )
          .selectAll(".hover-circle")
          .transition() // 开始过渡
          .duration(300) // 过渡持续时间
          .style("opacity", 0) // 逐渐变为透明
          .remove(); // 过渡结束后移除

        // 恢复连线的颜色和宽度
        const filteredLines = d3
          .selectAll("line") // 假设您使用 <line> 元素来表示连线
          .filter(
            (l) =>
              (l.source.id === link.source.id &&
                l.target.id === link.target.id) ||
              (l.source.id === link.target.id && l.target.id === link.source.id)
          );
        filteredLines
          .transition()
          .duration(300)
          .style("stroke", "lightgreen") // 恢复连线颜色为黑色
          .style("stroke-width", 2); // 恢复连线宽度
      }
    });
  });

simulation.nodes(graph.nodes).on("tick", ticked);

simulation.force("link").links(graph.links);

function ticked() {
  link
    .attr("x1", (d) => d.source.x)
    .attr("y1", (d) => d.source.y)
    .attr("x2", (d) => d.target.x)
    .attr("y2", (d) => d.target.y);

  node.attr("transform", (d) => `translate(${d.x - 30},${d.y - 30})`);
}

function dragstarted(event, d) {
  if (!event.active) simulation.alphaTarget(0.3).restart();
  d.fx = d.x;
  d.fy = d.y;
}

function dragged(event, d) {
  d.fx = event.x;
  d.fy = event.y;
}

function dragended(event, d) {
  if (!event.active) simulation.alphaTarget(0);
  d.fx = null;
  d.fy = null;
}
