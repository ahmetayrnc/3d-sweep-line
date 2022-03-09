# Duplicate is added for turtle to draw the shape 
# We don't actually need it --> (0,0)
shapes = {
  'rectangle': [(0,0,0),(0,1,0),(1,1,0),(1,0,0),(0,0,0)],
  'triangle': [(0,0,0),(0.5,1,0),(1,0,0),(0,0,0)],
}

def morphing(shape1,shape2,t): 
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
  # 3rd dimension is added (vertexZ)
  for i in range(0,numberOfvertices):
    vertexX=shape1Coordinates[i][0] + (shape2Coordinates[i][0]-shape1Coordinates[i][0])*t/10
    vertexY=shape1Coordinates[i][1] + (shape2Coordinates[i][1]-shape1Coordinates[i][1])*t/10
    vertexZ=shape1Coordinates[i][2] + (shape2Coordinates[i][2]-shape1Coordinates[i][2])*t/10
    vertices.append([vertexX,vertexY,vertexZ])
  return vertices
    
# Inputs  
shape1 = 'rectangle'
shape2 = 'triangle'
t = 5

morphedShapeCoordinates = morphing(shape1, shape2,t)
print(morphedShapeCoordinates)

