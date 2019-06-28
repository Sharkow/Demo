package SHroot.ViewersPack.Alternativa3D
{
	import SHroot.Service.SHEvent;
	
	import alternativa.engine3d.core.Object3D;
	
	/**Отправляется после полной загрузки компонента.
	 * Отправляющий объект доступен в target3D.
	 * @eventType SHEvent.COMPLETE*/
	[Event(name="shComplete", type="SHroot.Service.SHEvent")]
	
	/**Объект для отображения данных изображений в виде сферы.
	 * Может быть задано любое количество изображений.
	 * 
	 * @author Sharkow */
	public class ImageSphere extends Object3D
	{
		protected const IMAGE_WIDTH:int = 300, IMAGE_HEIGHT:int = 200,
			MIN_VERTICAL_PADDING:int = 36, MIN_HORIZONTAL_PADDING:int = 22;
		
		/**При малом количестве изображений есть смысл размещать два из них сверху и снизу сферы.
		 * Это будет происходить при количестве меньше или равном данной константе и больше IMAGES_MIN_SECTORS.*/
		protected const IMAGES_MAX_TOP_BOTTOM:int = 40;
		
		/**При очень малом количестве изображений их лучше размещать в одном секторе, то есть "каруселью".
		 * Это будет происходить при количестве изображений меньше или равном данной константе.*/		
		protected const IMAGES_MIN_SECTORS:int = 10;
		
		/**Радиус сферы рассчитывается на основе её площади, а площадь - на основе суммарной площади изображений.
		 * Но площадь сферы должна быть немного больше площади изображений. Настраиваем это данной константой.*/
		private const SPHERE_AREA_DEFAULT_ENLARGE:Number = 1.21;
		
		/**Число, которое прибавляется к площади изображений для расчета площади сферы.
		 * Площадь сферы нелинейно зависит от площади изображений.*/
		private const SPHERE_AREA_ADDITION:int = 1300000;
		
		/**Изображения располагаются не сразу от верхней и нижней точки сферы, а с некоторым отступом.*/
		private const ANGLE_OFFSET:int = 3;
		
		private var _images:Vector.<SHImageAlt3D> = new Vector.<SHImageAlt3D>(),
			r:int, loadedImages:int = 0,
			areaEnlarge:Number = SPHERE_AREA_DEFAULT_ENLARGE,
			coordinatesCounted:Boolean = false, completeDispatched:Boolean = false;
		
		/**Зенитные углы секторов. Длина массива равна посчитанному количеству секторов и 
		 * содержит зенитные углы для каждого сектора. Углы считаются от радиуса, расположенного вертикально.
		 * Так, для сектора, расположенного по экватору сферы, угол равен 90.*/
		protected var zenithAngles:Vector.<Number> = new Vector.<Number>();
		
		/**Азимуты секторов. Для каждого сектора содержит массив углов поворота, под которыми расположены изображения.*/
		protected var azimuthAngles:Vector.<Vector.<Number>> = new Vector.<Vector.<Number>>();
		
		/**Располагать изображения следует исходя из радиуса вписанной, а не описанной окружности.
		Считаем его на основе наибольшего "сектора" сферы.*/
		internal var innerRadius:int;
		
		
		/**Радиус сферы.
		 * Зависит от количества входных изображений и настроек их размеров.*/
		public function get radius():int{
			return r;
		}
		
		public function get images():Vector.<SHImageAlt3D>{
			return _images;
		}
		
		
		/**@param images
		 * Количество изображений может быть любым.
		 * Если их будет мало для сферы, то они будут расположены в плоскости или "каруселью". */
		public function ImageSphere(images:Array)
		{
			super();
			
			for (var i:int = 0; i<images.length; i++){
				var image:SHImageAlt3D = new SHImageAlt3D(images[i], IMAGE_WIDTH, IMAGE_HEIGHT);
				_images.push(image);
				image.addEventListener(SHEvent.COMPLETE, handleImgComplete);
			}
			countCoordinates();
		}
		
		
		private function handleImgComplete(event:SHEvent):void{
			(event.target3D as SHImageAlt3D).removeEventListener(SHEvent.COMPLETE, handleImgComplete);
			if(++loadedImages == _images.length)
				show();
		}
		
		private function show():void{
			for each (var image:SHImageAlt3D in _images)
				addChild(image);
			if(coordinatesCounted && (!completeDispatched)){
				completeDispatched = true;
				dispatchEvent(new SHEvent(SHEvent.COMPLETE, this));
			}				
		}
		
		private function countCoordinates():void{
			if(_images.length <= 1){
				r = innerRadius = IMAGE_WIDTH/2;
				coordinatesCounted = true;
				if ((loadedImages == _images.length) && (!completeDispatched)){
					completeDispatched = true;
					dispatchEvent(new SHEvent(SHEvent.COMPLETE, this));
				}
				return;
			}
			if(_images.length == 2){
				r = innerRadius = IMAGE_WIDTH + MIN_HORIZONTAL_PADDING/2;
				_images[0].x = - (IMAGE_WIDTH + MIN_HORIZONTAL_PADDING) / 2;
				_images[1].x = (IMAGE_WIDTH + MIN_HORIZONTAL_PADDING) / 2;
				
				coordinatesCounted = true;
				if ((loadedImages == _images.length) && (!completeDispatched)){
					completeDispatched = true;
					dispatchEvent(new SHEvent(SHEvent.COMPLETE, this));
				}
				return;
			}
			
			//Размещать ли изображения снизу и сверху сферы.
			var imagesTopBottom:Boolean = (_images.length <= IMAGES_MAX_TOP_BOTTOM && _images.length > IMAGES_MIN_SECTORS);
			
			//При расчете площади учитываем вертикальное и горизонтальное расстояние между изображениями.
			//Площадь сферы считается приближенно, но получается немного больше необходимого, этого достаточно.
			var imagesArea:Number = ((IMAGE_WIDTH * IMAGE_HEIGHT) + (IMAGE_WIDTH * MIN_VERTICAL_PADDING / 2) + (IMAGE_HEIGHT * MIN_HORIZONTAL_PADDING /2)) * (_images.length - (imagesTopBottom?1:0));
			r = Math.sqrt((SPHERE_AREA_ADDITION + imagesArea * areaEnlarge) / Math.PI / 4);
			
			//Сначала посчитать количество вертикальных секторов
			var sinHalfMinAngle:Number = (IMAGE_HEIGHT + MIN_VERTICAL_PADDING) / 2 / r;
			var minAngle:Number = (Math.asin(sinHalfMinAngle) * 2)* 180/Math.PI;
			
			/*Изображения располагаются не от верхней и нижней точки сферы,
			а с отступом длиной в собственную высоту. Этому отступу соответствует minAngle/2.
			Константа ANGLE_OFFSET задаёт дополнительный отступ.*/
			var offsetAngle:Number = minAngle/2 + ANGLE_OFFSET;
			
			//Считаем, что в любом случае самый маленький сектор должен вместить хотя бы три фотографии.
			var radiusFor3Images:Number = (IMAGE_WIDTH + MIN_HORIZONTAL_PADDING)/Math.sqrt(3);
			var minAngleFor3Images:Number = Math.asin(radiusFor3Images / r) *180/Math.PI;
			
			if (offsetAngle < minAngleFor3Images)
				offsetAngle = minAngleFor3Images;
			
			var sectors:int = (_images.length <= IMAGES_MIN_SECTORS) ? 1 : Math.floor((180 - offsetAngle*2) / minAngle);
			
			var angle:Number = (180 - offsetAngle*2) / sectors;//Вот это уже угол между вертикальными секторами.
			
			var sectorRadius:int = 0,//Радиус окружности в данном секторе.
				currentZenith:Number = 0,//Зенитный угол для данного
				currentAzimuthDelta:Number = 0,//Угол между изображениями в данном секторе.
				imagesInSector:int = 0,//Количество изображений в данном секторе.
				totalPlaces:int = 0;//Количество мест для изображений
			for (var sector:int = 0; sector < sectors; sector++){
				currentZenith = offsetAngle + angle/2 + angle*sector;
				zenithAngles.push(currentZenith);
				
				var adjustedZenith:Number = currentZenith;//Радиус сектора необходимо считать на основе граней изображений, ближних к "краю" сферы. Потому что эти грани оказываются ближе друг к другу, но они не должны пересекаться.
				adjustedZenith += Math.asin((IMAGE_HEIGHT + MIN_VERTICAL_PADDING) / 2 / r) * 180/Math.PI * (currentZenith>90?1:-1);
				
				sectorRadius = Math.round(Math.abs(r * Math.cos((90 - adjustedZenith) * Math.PI/180)));
				
				sinHalfMinAngle = (IMAGE_WIDTH + MIN_HORIZONTAL_PADDING) / 2 / sectorRadius;
				minAngle = Math.asin(sinHalfMinAngle) * 2 * 180/Math.PI;
				imagesInSector = Math.min(Math.floor(360/minAngle), _images.length - totalPlaces - (imagesTopBottom?2:0));
				
				currentAzimuthDelta = 360 / imagesInSector;
				totalPlaces += imagesInSector;
				
				azimuthAngles.push(new Vector.<Number>());
				for (var i:int = 0; i < imagesInSector; i++)
					azimuthAngles[sector].push(currentAzimuthDelta * i - 90);
				
				if(totalPlaces == (_images.length - (imagesTopBottom?2:0)))
					break;
			}
			if(totalPlaces < (_images.length - (imagesTopBottom?2:0))){
				trace("Too small area was assumed! Recounting...");
				zenithAngles.length = 0;
				azimuthAngles.length = 0;
				areaEnlarge += ((_images.length <= IMAGES_MIN_SECTORS) ? 4 : .03);
				countCoordinates();
				return;
			}
			
			var maxSides:int = 0;
			for (i = 0; i<azimuthAngles.length; i++)
				maxSides = Math.max(maxSides, azimuthAngles[i].length);
			
			innerRadius = Math.round((IMAGE_WIDTH + MIN_HORIZONTAL_PADDING) / (2 * Math.tan(Math.PI/maxSides)));
			
			if(imagesTopBottom){
				_images[0].rotationX = (-90) * Math.PI/180;
				_images[0].x = 0;
				_images[0].z = 0;
				_images[0].y = -innerRadius;
				
				_images[_images.length-1].rotationX = 90 * Math.PI/180;
				_images[_images.length-1].x = 0;
				_images[_images.length-1].z = 0;
				_images[_images.length-1].y = innerRadius;
			}
			
			var currentZenithIndex:int = 0,
				currentAzimuthIndex:int = 0;
				
			for (i = (imagesTopBottom?1:0); i < _images.length - (imagesTopBottom?1:0); i++){
				if(currentAzimuthIndex >= azimuthAngles[currentZenithIndex].length){
					currentZenithIndex++;
					currentAzimuthIndex = 0;
				}
				
				_images[i].rotationX = (zenithAngles[currentZenithIndex] - 90) * Math.PI/180;
				_images[i].rotationY = (270 - azimuthAngles[currentZenithIndex][currentAzimuthIndex]) * Math.PI/180;
				_images[i].x = innerRadius * Math.sin(zenithAngles[currentZenithIndex] * Math.PI/180) * Math.cos(azimuthAngles[currentZenithIndex][currentAzimuthIndex] * Math.PI/180);
				_images[i].z = innerRadius * Math.sin(zenithAngles[currentZenithIndex] * Math.PI/180) * Math.sin(azimuthAngles[currentZenithIndex][currentAzimuthIndex] * Math.PI/180);
				_images[i].y = - innerRadius * Math.cos(zenithAngles[currentZenithIndex] * Math.PI/180);
				currentAzimuthIndex++;
			}
			
			coordinatesCounted = true;
			if ((loadedImages == _images.length) && (!completeDispatched)){
				completeDispatched = true;
				dispatchEvent(new SHEvent(SHEvent.COMPLETE, this));
			}
		}
	}
}