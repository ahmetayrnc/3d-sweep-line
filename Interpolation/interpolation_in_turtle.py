import turtle
from time import sleep
import random

# Duplicate is added for turtle to draw the shape 
# We don't actually need it --> (0,0)
shapes = {
  'rectangle': [(0,0),(0,1),(1,1),(1,0),(0,0)],
  'triangle': [(0,0),(0.5,1),(1,0),(0,0)],
}

Pen = turtle.Turtle()
Pen.hideturtle()
Pen.tracer(0)
Pen.speed(0)
window = turtle.Screen()
window.bgcolor("#000000")
Pen.pensize(4)
Pen.color("#FFFFFF")

# Generates a random polygon with n vertices (only for testing)
def randomPolygonGenerator(n):
  polygon = []
  for vertex in range(n):
    x = random.uniform(0, 1)
    y = random.uniform(0, 1)
    polygon.append((x,y))
  polygon.append(polygon[0])
  return(polygon)

def morphing(shape1,shape2,t,x,y):
  Pen.penup()
  Pen.goto(x,y)
  Pen.pendown()
  
  shape1Coordinates=shapes[shape1]
  shape2Coordinates=shapes[shape2]
  
  # Finds the shape with less no. of vertices and repeatedly adds 
  # the last vertex of that shape into its coordinates to match 
  # the no. of vertices in both shapes (like a virtual vertex)
  if len(shape1Coordinates)>len(shape2Coordinates):
    lastVertex = shape2Coordinates[len(shape2Coordinates)-1]
    for i in range(0,len(shape1Coordinates)-len(shape2Coordinates)):
      shape2Coordinates.append(lastVertex)
  elif len(shape1Coordinates)<len(shape2Coordinates):
    lastVertex = shape1Coordinates[len(shape1Coordinates)-1]
    for i in range(0,len(shape2Coordinates)-len(shape1Coordinates)):
      shape1Coordinates.append(lastVertex)
   
  numberOfvertices = len(shape1Coordinates)
  vertices=[]

  # Morphing Calculations
  # Calculate the new interim coordinates of each node
  # Value 10 can be changed according to our need
  for i in range(0,numberOfvertices):
    vertexX=shape1Coordinates[i][0] + (shape2Coordinates[i][0]-shape1Coordinates[i][0])*t/10
    vertexY=shape1Coordinates[i][1] + (shape2Coordinates[i][1]-shape1Coordinates[i][1])*t/10
    vertices.append([vertexX,vertexY])
    
  # Draw the resulting morphed shape
  Pen.penup()
  for vertex in vertices:
    Pen.goto(x + vertex[0]*200, y + vertex[1]*200)
    Pen.pendown()
        
randomPolygon = randomPolygonGenerator(5)
shapes['randomPolygon'] = randomPolygon

# Inputs  
shape1 = 'rectangle'
shape2 = 'triangle'
t = 5

Pen.clear()
morphing(shape1, shape2,t,-100,-100)
sleep(0.05)
Pen.getscreen().update() 

# Animation
""" 
shape1 = 'rectangle'
shape2 = 'triangle'
while True:
  for t in range(0,11):
    Pen.clear()
    morphing(shape1, shape2,t,-100,-100)
    sleep(0.05)
    Pen.getscreen().update() 
  
  sleep(0.5) """